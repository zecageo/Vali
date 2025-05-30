﻿using System.Collections.Concurrent;
using static Vali.Core.LocationLakeMapGenerator;

namespace Vali.Core;

public class LocationReader
{
    private static readonly ConcurrentDictionary<string, LocationLakeMapGenerator.GeoMapLocation[]> Locations = new();
    private static readonly object LockObject = new object();

    public static LocationLakeMapGenerator.GeoMapLocation[] DeserializeLocationsFromFile(string path)
    {
        if (Locations.TryGetValue(path, out var cachedLocations))
        {
            return cachedLocations;
        }

        lock (LockObject)
        {
            if (Locations.TryGetValue(path, out cachedLocations))
            {
                return cachedLocations;
            }

            var extension = Path.GetExtension(path);
            if (extension == ".csv")
            {
                var lines = File.ReadAllLines(path);
                return lines.Where(x => x.Length > 2).Select(x =>
                        new LocationLakeMapGenerator.GeoMapLocation
                        {
                            lat = x.Split(',')[0].ParseAsDouble(),
                            lng = x.Split(',')[1].ParseAsDouble(),
                        })
                    .ToArray();
            }

            var firstChar = Extensions.ReadChars(path, 1);
            LocationLakeMapGenerator.GeoMapLocation[] mapLocations;
            if (firstChar[0] == '[')
            {
                var locations = Extensions.DeserializeJsonFromFile<GeoMapLocation[]>(path) ?? throw new InvalidOperationException("Invalid location json structure.");
                mapLocations = locations.Where(x => x.lat is not null && x.lng is not null).Select(x => new LocationLakeMapGenerator.GeoMapLocation
                {
                    lat = x.lat!.Value,
                    lng = x.lng!.Value,
                    panoId = x.panoId,
                    countryCode = x.countryCode,
                    subdivisionCode = x.subdivisionCode,
                    extra = x.extra,
                    heading = x.heading,
                    pitch = x.pitch,
                    zoom = x.zoom,
                }).ToArray();
            }
            else
            {
                var map = Extensions.DeserializeJsonFromFile<MapWithDistributionLocations>(path) ?? throw new InvalidOperationException("Invalid map json structure.");
                mapLocations = map.customCoordinates.Where(x => x.lat is not null && x.lng is not null).Select(x => new LocationLakeMapGenerator.GeoMapLocation
                {
                    lat = x.lat!.Value,
                    lng = x.lng!.Value,
                    panoId = x.panoId,
                    countryCode = x.countryCode,
                    subdivisionCode = x.subdivisionCode,
                    extra = x.extra,
                    heading = x.heading,
                    pitch = x.pitch,
                    zoom = x.zoom,
                }).ToArray();
            }

            Locations.TryAdd(path, mapLocations);
            return mapLocations;
        }
    }

    record MapWithDistributionLocations
    {
        public string name { get; set; } = "";
        public GeoMapLocation[] customCoordinates { get; set; } = [];
    }

    record GeoMapLocation
    {
        public double? lat { get; set; }
        public double? lng { get; set; }
        public double heading { get; set; }
        public double? zoom { get; set; }
        public double? pitch { get; set; }
        public GeoMapLocationExtra? extra { get; set; }
        public string? panoId { get; set; }
        public string? countryCode { get; set; }
        public string? subdivisionCode { get; set; }
    }
}