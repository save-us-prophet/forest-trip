﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.Wpf;

using Newtonsoft.Json;

using ShareInvest.Data;
using ShareInvest.EventHandler;
using ShareInvest.Models;

using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ShareInvest;

class CoreWebView
{
    public event EventHandler<EventArgs>? Send;

    internal async Task OnInitializedAsync(string? uriString = null)
    {
        if (webView.Source != null)
        {
            return;
        }
        await webView.EnsureCoreWebView2Async();

        ConnectEvents();

        if (string.IsNullOrEmpty(uriString) is false)
        {
            webView.Source = new Uri(uriString);
        }
    }

    internal CoreWebView(WebView2 webView)
    {
        this.webView = webView;
    }

    void ConnectEvents()
    {
        webView.NavigationStarting += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.NavigationStarting), args);
#endif            
        };

        webView.DataContextChanged += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.DataContextChanged), args.NewValue);
#endif
        };

        webView.SourceChanged += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.SourceChanged), args);
#endif
        };

        webView.ContentLoading += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.ContentLoading), args);
#endif
        };

        webView.CoreWebView2.HistoryChanged += (sender, e) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.CoreWebView2.HistoryChanged), e);
#endif
        };

        webView.CoreWebView2.DOMContentLoaded += (sender, e) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.CoreWebView2.DOMContentLoaded), e);
#endif            
        };

        webView.NavigationCompleted += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.NavigationCompleted), args);
#endif
        };

        webView.CoreWebView2.WebResourceResponseReceived += async (sender, args) =>
        {
            if (200 == args.Response.StatusCode && args.Request.Headers.Any())
            {
                var name = $"{Guid.NewGuid()}";

                try
                {
                    if (args.Request.Headers.GetHeader("Host") is string host)
                    {
                        name = host.Replace(".", "-");
                    }
                }
                catch
                {
                    if (args.Request.Headers.GetHeader("Referer") is string referer)
                    {
                        name = referer.Split('/')[^1].Replace(".", "-").Replace("?", "-");

                        if (name.Length > 0x100 - 11)
                        {
                            name = name[..(0x100 - 11)];
                        }
                    }
                }

                if (JsonConvert.DeserializeObject(await webView.ExecuteScriptAsync(Properties.Resources.HTML)) is string html)
                {
#if DEBUG
                    using (var sw = new StreamWriter(Path.Combine(Properties.Resources.TAG, $"{name}.html")))
                    {
                        sw.WriteLine(html);
                    }
#endif
                }

                if ("main-do".Equals(name))
                {
                    var tag = await webView.ExecuteScriptAsync(Properties.Resources.LOCATION);

                    if (string.IsNullOrEmpty(tag) || "\"[]\"".Equals(tag))
                    {
                        return;
                    }
                    tag = tag.Replace("\\n", "\n").Replace("\\", string.Empty).Replace("\n", string.Empty).Trim('"');

                    foreach (var text in JsonConvert.DeserializeObject<List<string>>(tag) ?? [])
                    {
                        var strArr = text.Split('(');

                        Send?.Invoke(this, new LocationArgs(new LocItem
                        {
                            LocName = strArr[0],
                            Count = Convert.ToInt32(strArr[1][..^1])
                        }));
                    }
                }

                if ("www-foresttrip-go-kr".Equals(name))
                {
                    var region = await webView.ExecuteScriptAsync(Properties.Resources.REGION);
                    var listHomeItems = await webView.ExecuteScriptAsync(Properties.Resources.HOUSE);

                    if (!string.IsNullOrEmpty(listHomeItems))
                    {
                        region = region.Replace("\"", string.Empty);

                        using (var context = new ForestTripContext())
                        {
                            foreach (var item in JsonConvert.DeserializeObject<List<ForestRetreat>>(listHomeItems) ?? [])
                            {
                                if (string.IsNullOrEmpty(item.Id)) continue;

                                var resort = new ForestRetreat
                                {
                                    Id = item.Id,
                                    Name = item.Name,
                                    Region = region
                                };

                                if (context.ForestRetreat.Find(item.Id) is ForestRetreat fr)
                                {
                                    fr.Name = item.Name;
                                    fr.Region = region;
                                }
                                else
                                {
                                    context.ForestRetreat.Add(resort);
                                }
                                Send?.Invoke(this, new HouseArgs(resort));
                            }
                            _ = context.SaveChanges();
                        }
                    }
                    ForestRetreat? forestRetreat = null;

                    try
                    {
                        var resort = await webView.ExecuteScriptAsync(Properties.Resources.RESORT);

                        if (!string.IsNullOrEmpty(resort) && !"\"[]\"".Equals(resort) && !"null".Equals(resort))
                        {
                            foreach (var fr in JsonConvert.DeserializeObject<Region[]>(resort.Replace("\\\"", "\"")[1..^1]) ?? [])
                            {
                                if (!"산림휴양시설".Equals(fr.Title) || string.IsNullOrEmpty(fr.Text))
                                {
                                    continue;
                                }

                                using (var context = new ForestTripContext())
                                {
                                    forestRetreat = context.ForestRetreat.AsNoTracking().FirstOrDefault(e => !string.IsNullOrEmpty(e.Name) && e.Name.EndsWith(fr.Text));
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }

                    try
                    {
                        var list = JsonConvert.DeserializeObject<List<string>>((await webView.ExecuteScriptAsync(Properties.Resources.CABIN)).Replace("\\\"", "\"")[1..^1]) ?? [];

                        using (var context = new ForestTripContext())
                        {
                            foreach (var e in list)
                            {
                                var id = forestRetreat?.Id;
                                var cabinName = e.Replace("사용가능 시설", string.Empty).Replace("\\n", string.Empty).Trim();

                                if (!string.IsNullOrEmpty(id) && context.Cabin.Find(id, cabinName) is null)
                                {
                                    context.Cabin.Add(new Cabin
                                    {
                                        Id = id,
                                        Name = cabinName,
                                        Region = region
                                    });
                                }
                            }
                            _ = context.SaveChanges();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        };

        webView.WebMessageReceived += (sender, args) =>
        {
#if DEBUG
            WriteLine(sender, nameof(webView.WebMessageReceived), args);
#endif
        };
    }

    [Conditional("DEBUG")]
    static void GetProperites<T>(T property) where T : class
    {
        foreach (var propertyInfo in property.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            Debug.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(property)}");
        }
    }

    [Conditional("DEBUG")]
    static void WriteLine<T>(object? sender, string name, T property) where T : class
    {
        Debug.WriteLine(' ');
        Debug.WriteLine(sender, name);

        GetProperites(property);
    }

    readonly WebView2 webView;
}