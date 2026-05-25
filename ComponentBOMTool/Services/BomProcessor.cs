using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ComponentBOMTool.Models;

namespace ComponentBOMTool.Services
{
    public class BomProcessor
    {
        private readonly MouserService _mouser;
        
        public BomProcessor(string apiKey)
        {
            _mouser = new MouserService(apiKey);
        }

        public async Task<List<BomItem>> ProcessAsync(List<BomItem> input)
        {
            var results = new List<BomItem>();

            foreach (var item in input)
            {
                var r = await ProcessItem(item);
                results.Add(r);
            }

            return results;
        }

        private async Task<BomItem> ProcessItem(BomItem item)
        {
            try
            {
                JObject data = await _mouser.SearchAsync(item.PartNumber);

                var parts = data?["SearchResults"]?["Parts"] as JArray;

                if (parts == null || !parts.Any())
                    return CreateEmpty(item);

                var main = parts.FirstOrDefault(p =>
                    string.Equals(
                        p["ManufacturerPartNumber"]?.ToString(),
                        item.PartNumber,
                        StringComparison.OrdinalIgnoreCase
                    )
                ) ?? parts[0];

                string manufacturer = main["Manufacturer"]?.ToString() ?? "N/A";

                string lifecycle =
                    main["LifecycleStatus"]?.ToString()
                    ?? main["ProductStatus"]?.ToString()
                    ?? "None";

                string stock = main["Availability"]?.ToString() ?? "N/A";

                string priceStr = main["PriceBreaks"]?.First?["Price"]?.ToString();
                decimal? unitPrice = SafeDecimal(priceStr);

                decimal? totalPrice = unitPrice.HasValue
                    ? unitPrice * item.Quantity
                    : null;

                // ✅ alternates (streamlit same)
                var alternatesList = parts
                    .Skip(1)
                    .Select(p => p["ManufacturerPartNumber"]?.ToString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                string alternates = alternatesList.Any()
                    ? string.Join(", ", alternatesList)
                    : null;

                return new BomItem
                {
                    PartNumber = item.PartNumber,
                    Quantity = item.Quantity,
                    Manufacturer = manufacturer,
                    Lifecycle = lifecycle,
                    Stock = stock,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Alternates = alternates
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private decimal? SafeDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            try
            {
                input = input.Replace("$", "").Replace(",", "").Trim();
                return decimal.Parse(input);
            }
            catch
            {
                return null;
            }
        }

        private BomItem CreateEmpty(BomItem item)
        {
            return new BomItem
            {
                PartNumber = item.PartNumber,
                Quantity = item.Quantity,
                Manufacturer = "N/A",
                Lifecycle = "None",
                Stock = "N/A"
            };
        }
    }
}
