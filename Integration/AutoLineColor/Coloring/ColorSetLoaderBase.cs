using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal abstract class ColorSetLoaderBase : IColorSetLoader
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [NotNull]
        public string Name { get; }

        [NotNull] private readonly string _filename;
        [NotNull] private readonly string _defaultContent;

        protected ColorSetLoaderBase([NotNull] string name, [NotNull] string filename, [NotNull] string defaultContent)
        {
            Name = name;
            _filename = filename;
            _defaultContent = defaultContent;
        }

        public IColorSet LoadColorSet()
        {
            var logger = Console.Instance;

            // we need to load the color list
            var modConfigPath = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, Path.Combine("ModsSettings", "IPT"));
            var fullPath = Path.Combine(modConfigPath, _filename);
            logger.Message($"Loading color set from {fullPath}");
            var unparsedColors = _defaultContent;

            try
            {
                if (File.Exists(fullPath))
                {
                    unparsedColors = File.ReadAllText(fullPath);
                }
                else
                {
                    logger.Message("No colors found, writing default values to  " + fullPath);
                    Directory.CreateDirectory(modConfigPath);
                    File.WriteAllText(fullPath, unparsedColors);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error reading colors from disk: " + ex);
            }

            try
            {
                return ParseColorSet(unparsedColors);
            }
            catch (Exception ex)
            {
                logger.Error("Error parsing loaded colors: " + ex);
                logger.Message("Color file content: " + unparsedColors);
                throw;
            }
        }

        [NotNull] protected abstract IColorSet ParseColorSet([NotNull] string unparsedColors);

        protected static bool TryHexToColor(string hex, out Color32 color)
        {
            try
            {
                hex = hex.Replace("0x", ""); //in case the string is formatted 0xFFFFFF
                hex = hex.Replace("#", ""); //in case the string is formatted #FFFFFF
                hex = hex.Trim();

                byte alpha = 255; //assume fully visible unless specified in hex

                var red = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var green = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var blue = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

                //Only use alpha if the string has enough characters
                if (hex.Length == 8)
                {
                    alpha = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                }

                color = new Color32(red, green, blue, alpha);
                return true;
            }
            catch (Exception)
            {
                color = new Color32(0, 0, 0, 255);
                return false;
            }
        }
    }
}
