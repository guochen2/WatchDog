using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Furion.ConfigurableOptions;

using Microsoft.Extensions.Configuration;

namespace WatchDog.src.Options;
public class WatchDogOptions : IConfigurableOptionsListener<WatchDogOptions>
{
    public string PathBlacklist { get; set; }
    public string ReqHeaderBlacklist { get; set; }
    public string ResHeaderBlacklist { get; set; }
    public string ExternalWhitelists { get; set; }
    public void OnListener(WatchDogOptions options, IConfiguration configuration)
    {
        Models.WatchDogConfigModel.Blacklist = String.IsNullOrEmpty(options.PathBlacklist) ? new string[] { } : options.PathBlacklist.Replace(" ", string.Empty).Split(',');
        Models.WatchDogConfigModel.ResHeaderBlacklist = String.IsNullOrEmpty(options.ResHeaderBlacklist) ? new string[] { } : options.ResHeaderBlacklist.Replace(" ", string.Empty).Split(',');
        Models.WatchDogConfigModel.ReqHeaderBlacklist = String.IsNullOrEmpty(options.ReqHeaderBlacklist) ? new string[] { } : options.ReqHeaderBlacklist.Replace(" ", string.Empty).Split(',');
        Models.WatchDogConfigModel.ExternalWhitelists = String.IsNullOrEmpty(options.ExternalWhitelists) ? new string[] { } : options.ExternalWhitelists.Replace(" ", string.Empty).Split(',');
        Console.WriteLine($"WatchDog实时变更:{options.PathBlacklist}");
    }

    public void PostConfigure(WatchDogOptions options, IConfiguration configuration)
    {

    }
}
