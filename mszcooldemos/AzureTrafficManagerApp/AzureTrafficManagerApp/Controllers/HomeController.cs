using AzureTrafficManagerApp.AvailabilityRepository;
using AzureTrafficManagerApp.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace AzureTrafficManagerApp.Controllers
{
    public class HomeController : Controller
    {
        public const string SETTING_STORAGE_CONNECTION_STRING_NAME = "StorageConnectionStringSetting";
        public const string SETTING_BACKGROUND_COLOR = "BackgroundColorSetting";
        public const string SETTING_LOCATION_NAME = "LocationNameSetting";

        private string _location;
        private string _backgroundColor;
        private IAvailabilityRepository _repository;

        public HomeController()
        {
            if (RoleEnvironment.IsAvailable)
            {
                _repository = AvailabilityRepositoryFactory.CreateRepository
                                    (
                                        SupportedRepositories.Table,
                                        CloudConfigurationManager.GetSetting(SETTING_STORAGE_CONNECTION_STRING_NAME)
                                    );

                _location = CloudConfigurationManager.GetSetting(SETTING_LOCATION_NAME);
                _backgroundColor = CloudConfigurationManager.GetSetting(SETTING_BACKGROUND_COLOR);
            }
            else
            {
                _repository = null;
            }
        }

        public ActionResult Index()
        {
            try
            {
                //
                // Allowed to run in a cloud service, only!!
                //
                if (_repository == null)
                {
                    ViewBag.ErrorMessage = "Not running in cloud environment, this must run as a cloud service!!";
                    return View("Error");
                }

                //
                // Read the availability table and settings and correct them if needed
                //
                var availabilityEntries = _repository.GetServices();
                foreach (var item in availabilityEntries)
                {
                    if (DateTime.UtcNow.Subtract(item.LastPingTime) > TimeSpan.FromSeconds(30))
                    {
                        item.IsOnline = false;
                        _repository.UpdateServiceAvailability(item.ServiceName, false, item.IsSetToOffline);
                    }
                }

                //
                // Create the ViewModel for display-work
                //
                var viewModel = new AvailabilityViewModel()
                {
                    Location = _location,
                    BackgroundColor = _backgroundColor,
                    ServiceStates = availabilityEntries
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        public ActionResult SwapOffline()
        {
            var service = _repository.GetService(_location);
            if (service == null)
                _repository.UpdateServiceAvailability(_location, false, true);
            else
                _repository.UpdateServiceAvailability(_location, service.IsOnline, !service.IsSetToOffline);

            return RedirectToAction("Index");
        }

        public ActionResult Monitoring()
        {
            var serviceEntry = _repository.GetService(_location);
            if (serviceEntry != null && serviceEntry.IsSetToOffline)
            {
                if (serviceEntry.IsOnline)
                    _repository.UpdateServiceAvailability(_location, false, true);
                throw new HttpException((int)HttpStatusCode.ServiceUnavailable, "Service is offline!");
            }

            _repository.UpdateServiceAvailability(_location, true, false);

            return base.Content("available", "text/plain");
        }
    }
}
