using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stankin3.Data;
using stankin3.Models;
using System.Globalization;

namespace stankin3.Controllers;

[ApiController]
[Route("[controller]")]
public class Stankin3Controller : ControllerBase
{
    private readonly DataContext _context;
    private readonly HttpClient _client;
    private readonly AppSettings _appsettings;

    public Stankin3Controller(DataContext context,
        HttpClient client,
        AppSettings appsettings)
    {
        _context = context;
        _client = client;
        _appsettings = appsettings;
    }

    [HttpPost("post")]
    public async Task<ActionResult> Post(int year)
    {
        try
        {
            var url = $"https://www.cnb.cz/en/financial_markets/foreign_exchange_market/exchange_rate_fixing/year.txt?year={year}";
            var res = await _client.GetAsync(url);
            var resStr = await res.Content.ReadAsStringAsync();
            resStr = resStr.Replace('|', ',');

            using (var reader = new StringReader(resStr))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<Rate>().ToArray();

                foreach(var record in records)
                {
                    foreach (var propName in typeof(Rate).GetProperties().Select(p => p.Name).Where(s => s != "Date"))
                    {
                        var prop = record.GetType().GetProperty(propName);
                        var amount = int.Parse(((NameAttribute[])prop.GetCustomAttributes(typeof(NameAttribute), false))[0].Names[0].Split(' ')[0]);
                        prop.SetValue(record, double.Parse(record.GetType().GetProperty(propName).GetValue(record).ToString()) / amount);
                    }
                }

                await _context.Rates.AddRangeAsync(records);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    [HttpPost("start-job")]
    public ActionResult StartJob()
    {
        try
        {
            var jobs = JobStorage.Current.GetConnection().GetRecurringJobs();

            if (jobs.Count == 0)
            {
                RecurringJob.AddOrUpdate<Job.Job>("fillRates", j => j.FillRatesAsync(), _appsettings.Cron);
                return Ok();
            }
            else
                return Problem("Job already exists");
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    [HttpPost("stop-job")]
    public ActionResult StopJob()
    {
        try
        {
            var jobs = JobStorage.Current.GetConnection().GetRecurringJobs();

            if (jobs.Count > 0)
            {
                RecurringJob.RemoveIfExists("fillRates");
                return Ok();
            }
            else
                return Problem("Job not found");
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    [HttpGet("get-stats")]
    public async Task<ActionResult<RateStats>> GetStats(string minDateStr, string maxDateStr, string currencyCode)
    {
        try
        {
            if (!DateTime.TryParse(minDateStr, out var minDate))
                return Problem("Min date parse error");

            if (!DateTime.TryParse(maxDateStr, out var maxDate))
                return Problem("Max date parse error");

            var rates = await _context.Rates.Where(r => r.Date >= minDate && r.Date <= maxDate).ToListAsync();

            if (rates.Count == 0)
                return Problem("No data");

            static double GetPropValue(object src, string propName) => double.Parse(src.GetType().GetProperty(propName).GetValue(src, null).ToString());

            return new RateStats
            {
                MinDate = rates.Min(r => r.Date).ToString("dd.MM.yyyy"),
                MaxDate = rates.Max(r => r.Date).ToString("dd.MM.yyyy"),
                AverageValue = rates.Average(r => GetPropValue(r, currencyCode)),
                MaxValue = rates.Max(r => GetPropValue(r, currencyCode)),
                MinValue = rates.Min(r => GetPropValue(r, currencyCode))
            };
        }
        catch (Exception e)
        {
            return Problem(e.Message);
        }
    }

    [HttpGet("get-currencies-codes")]
    public ActionResult<string[]> GetÑurrenciesÑodes() => typeof(Rate).GetProperties().Select(p => p.Name).Where(s => s != "Date").ToArray();
}