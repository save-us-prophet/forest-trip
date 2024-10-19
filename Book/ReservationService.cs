using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using ShareInvest.Models;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

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
        chromeService.HideCommandPromptWindow = true;

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

    internal async Task<Reservation?> EnterInfomationAsync(Reservation rm, int commandTimeout = 0x100)
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

        if (clickComboBox("//*[@id=\"srch_frm\"]/div[1]/div[1]", matchWord: rm.Region, suffix: "//*[@id=\"srch_region\"]/ul/li"))
        {
            foreach (var popup in driver.FindElements(By.ClassName("enterPopup")))
            {
                if (popup.GetAttribute("class").EndsWith("show"))
                {
                    try
                    {
                        foreach (var div in popup.FindElements(By.ClassName("ep_cookie_close")))
                        {
                            foreach (var a in div.FindElements(By.TagName("a")))
                            {
                                if ("day_close".Equals(a.GetAttribute("class")))
                                {
                                    a.Click();

                                    break;
                                }
                            }
                            await Task.Delay(0x100);
                        }
                    }
                    catch (ElementClickInterceptedException)
                    {

                    }
                    continue;
                }
            }

            if (clickComboBox("//*[@id=\"srch_frm\"]/div[2]/div[1]", matchWord: rm.ForestRetreat, suffix: "//*[@id=\"srch_rcfcl\"]/ul/li"))
            {
                if (clickComboBox("//*[@id=\"srch_frm\"]/div[3]"))
                {
                    var calendar = driver.FindElement(By.Id("forestCalPicker"));

                    calendar.FindElement(By.XPath($"//a[@name='{rm.StartDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(rm.StartDate.DayOfWeek)})']")).Click();
                    calendar.FindElement(By.XPath($"//a[@name='{rm.EndDate.ToString("d").Replace('-', '/')}({GetDayOfWeek(rm.EndDate.DayOfWeek)})']")).Click();

                    foreach (var a in calendar.FindElements(By.TagName("a")))
                    {
                        var tag = a.TagName;

                        if ("확인".Equals(a.Text))
                        {
                            a.Click();

                            break;
                        }
                    }

                    while (int.TryParse(driver.FindElement(By.Id("stng_nofpr")).Text, out int cost) && cost != rm.NumberOfPeople)
                    {
                        foreach (var a in driver.FindElement(By.XPath("//*[@id=\"srch_frm\"]/div[4]/div[2]")).FindElements(By.TagName("a")))
                        {
                            if (cost > rm.NumberOfPeople && "minus".Equals(a.GetAttribute("class")))
                            {
                                a.Click();

                                break;
                            }

                            if (cost < rm.NumberOfPeople && "plus".Equals(a.GetAttribute("class")))
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

                    if (string.IsNullOrEmpty(rm.CabinName) is false)
                    {
                        await Task.Delay(0x200);

                        if (driver.WindowHandles.Count > 1)
                        {
                            string originalHandle = driver.CurrentWindowHandle;

                            try
                            {
                                foreach (var handle in driver.WindowHandles)
                                {
                                    if (originalHandle.Equals(handle))
                                    {
                                        continue;
                                    }

                                    foreach (var div in driver.SwitchTo().Window(handle).FindElement(By.Id("mainContent")).FindElements(By.ClassName("cont_login")))
                                    {
                                        foreach (var e in div.FindElements(By.TagName(nameof(div))))
                                        {
                                            if ("login_kakaomail".Equals(e.GetAttribute("class"))) return null;
                                        }
                                    }
                                }
                            }
                            catch (NoSuchElementException)
                            {

                            }
                            finally
                            {
                                driver.SwitchTo().Window(originalHandle);
                            }
                        }

                        try
                        {
                            _ = driver.FindElement(By.Id("searchResultEmpty"));
                        }
                        catch (NoSuchElementException)
                        {
                            if (await ChooseCabinAsync(rm.CabinName))
                            {
                                await OpticalCharacterRecognitionAsync();
                                await Reserve();

                                rm.Result = true;
                            }
                        }
                    }
                }
            }
        }
        return rm;
    }

    async Task OpticalCharacterRecognitionAsync(int commandTimeout = 0x400)
    {
        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        await Task.Delay(0x200);

        var img = dw.Until(e => e.FindElement(By.Id("captchaImg")));

        foreach (var div in img.FindElement(By.XPath("..")).FindElement(By.XPath("..")).FindElements(By.TagName("div")))
        {
            if ("sp_right".Equals(div.GetAttribute("class")))
            {
                foreach (var a in div.FindElements(By.TagName("a")))
                {
                    if ("듣기".Equals(a.GetAttribute("title")))
                    {
                        a.Click();

                        break;
                    }
                }
                break;
            }
        }

        using (var content = new MultipartFormDataContent())
        {
            var imageContent = new ByteArrayContent(Convert.FromBase64String((string)((IJavaScriptExecutor)driver).ExecuteScript(Properties.Resources.CAPTURE, img)));

            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

            content.Add(imageContent, "file", "captcha.png");

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("http://localhost:15409", content);

                if (response.IsSuccessStatusCode)
                {
                    var captchaText = await response.Content.ReadAsStringAsync();

                    dw.Until(e => e.FindElement(By.Id("atmtcRsrvtPrvntChrct"))).SendKeys(captchaText);
                }
            }
        }
    }

    async Task Reserve(int commandTimeout = 0x400)
    {
        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        await Task.Delay(0x200);

        dw.Until(e => e.FindElement(By.Id("arr_01")).FindElement(By.XPath(".."))).Click();

        await Task.Delay(0x100);

        dw.Until(e => e.FindElement(By.Id("btnRsrvt"))).Click();

        await Task.Delay(0x200);

        dw.Until(e => e.SwitchTo().Alert()).Accept();
    }

    async Task<bool> ChooseCabinAsync(string name, int commandTimeout = 0x400)
    {
        await Task.Delay(0x200);

        var dw = new WebDriverWait(driver, TimeSpan.FromMilliseconds(commandTimeout));

        foreach (var label in dw.Until(e => e.FindElement(By.Id("cmpgr")).FindElements(By.ClassName("chackbox_all"))))
        {
            label.Click();

            await Task.Delay(0x200);
        }
        var cabins = dw.Until(e => e.FindElement(By.Id("gsrm")));

        foreach (var label in cabins.FindElements(By.ClassName("chackbox_all")))
        {
            label.Click();

            await Task.Delay(0x200);
        }

        foreach (var label in cabins.FindElements(By.TagName("label")))
        {
            if (name.Contains(label.Text))
            {
                label.Click();

                break;
            }
        }
        await Task.Delay(0x200);

        foreach (var div in driver.FindElements(By.ClassName("goods_list_area")))
        {
            foreach (var cl in div.FindElements(By.TagName("div")))
            {
                if (cl.GetAttribute("class").StartsWith("communication"))
                {
                    foreach (var e in cl.FindElements(By.TagName("div")))
                    {
                        if (e.GetAttribute("class").StartsWith("list") is false)
                        {
                            continue;
                        }

                        foreach (var a in e.FindElements(By.TagName("a")))
                        {
                            if ("item".Equals(a.GetAttribute("class")) && a.Text.Contains(name))
                            {
                                a.Click();

                                return true;
                            }
                        }
                    }
                    break;
                }
            }
        }
        return false;
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