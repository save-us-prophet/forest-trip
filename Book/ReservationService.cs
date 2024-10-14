using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using System.IO;

namespace ShareInvest;

class ReservationService : IDisposable
{
    public void Dispose()
    {
        driver.Dispose();

        chromeService.Dispose();
    }

    internal ReservationService(string url, string[]? args = null, int commandTimeout = 0x100)
    {
        if (args != null && args.Length > 0)
        {
            foreach (var arg in args) this.args.Enqueue(arg);
        }
        var chromeOption = new ChromeOptions
        {

        };
        chromeOption.AddArguments(this.args);

        driver = new ChromeDriver(chromeService, chromeOption, TimeSpan.FromSeconds(commandTimeout));

        if (this.args.Count < 4)
        {
            driver.Manage().Window.Maximize();
        }
        driver.Navigate().GoToUrl(url);
    }

    internal async Task EnterInfomationAsync(int np, string? region, string? house, DateTime startDate, DateTime endDate, int commandTimeout = 0x100)
    {
        bool clickComboBox(string prefix, string? suffix = null, string? matchWord = null)
        {
            foreach (var e in driver.FindElements(By.XPath(prefix)))
            {
                e.Click();

                if (string.IsNullOrEmpty(suffix))
                {
                    return true;
                }

                foreach (var li in new WebDriverWait(driver, TimeSpan.FromSeconds(commandTimeout * .25)).Until(e => e.FindElements(By.XPath(suffix))))
                {
                    var a = li.FindElement(By.TagName("a"));

                    if (a.Text.Trim().Equals(matchWord))
                    {
                        a.Click();

                        return true;
                    }
                }
            }
            return false;
        }

        if (clickComboBox("//*[@id=\"srch_frm\"]/div[1]/div[1]", matchWord: region, suffix: "//*[@id=\"srch_region\"]/ul/li"))
        {
            if (clickComboBox("//*[@id=\"srch_frm\"]/div[2]/div[1]", matchWord: house, suffix: "//*[@id=\"srch_rcfcl\"]/ul/li"))
            {
                if (clickComboBox("//*[@id=\"srch_frm\"]/div[3]"))
                {
                    var calendar = driver.FindElement(By.Id("forestCalPicker"));

                    calendar.FindElement(By.XPath($"//a[@name='{startDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(startDate.DayOfWeek)})']")).Click();
                    calendar.FindElement(By.XPath($"//a[@name='{endDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(endDate.DayOfWeek)})']")).Click();

                    foreach (var a in calendar.FindElements(By.TagName("a")))
                    {
                        var tag = a.TagName;

                        if ("확인".Equals(a.Text))
                        {
                            a.Click();

                            break;
                        }
                    }

                    while (int.TryParse(driver.FindElement(By.Id("stng_nofpr")).Text, out int cost) && cost != np)
                    {
                        foreach (var a in driver.FindElement(By.XPath("//*[@id=\"srch_frm\"]/div[4]/div[2]")).FindElements(By.TagName("a")))
                        {
                            if (cost > np && "minus".Equals(a.GetAttribute("class")))
                            {
                                a.Click();

                                break;
                            }

                            if (cost < np && "plus".Equals(a.GetAttribute("class")))
                            {
                                a.Click();

                                break;
                            }
                        }
                    }

                    foreach (var btn in driver.FindElement(By.XPath("//*[@id=\"srch_frm\"]/div[5]")).FindElements(By.TagName("button")))
                    {
                        if ("조회하기".Equals(btn.GetAttribute("title")))
                        {
                            btn.Click();

                            await Task.Delay(0x200);

                            foreach (var li in new WebDriverWait(driver, TimeSpan.FromSeconds(commandTimeout)).Until(e => e.FindElement(By.XPath("//*[@id=\"infoWrap\"]/fieldset/div/div[3]/ul"))).FindElements(By.TagName("li")))
                            {
                                foreach (var a in li.FindElements(By.TagName("a")))
                                {
                                    if ("kakao_Login".Equals(a.GetAttribute("class")))
                                    {
                                        a?.Click();

                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    static string GetDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Sunday => "일",
        DayOfWeek.Monday => "월",
        DayOfWeek.Tuesday => "화",
        DayOfWeek.Wednesday => "수",
        DayOfWeek.Thursday => "목",
        DayOfWeek.Friday => "금",
        DayOfWeek.Saturday or _ => "토"
    };

    readonly Queue<string> args = new(["--window-size=1920,1080", Properties.Resources.USERAGENT, string.Concat(Properties.Resources.USERDATA, Path.Combine(Environment.CurrentDirectory, Properties.Resources.USERPATH))]);

    readonly ChromeDriver driver;

    readonly ChromeDriverService chromeService = ChromeDriverService.CreateDefaultService();
}