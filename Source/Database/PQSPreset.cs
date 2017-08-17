﻿/**
 * Stellarator - Creates procedural systems for Kopernicus
 * Copyright (c) 2016 Thomas P.
 * Licensed under the Terms of the MIT License
 */

using ConfigNodeParser;
using Kopernicus.Configuration;

namespace Stellarator.Database
{
    /// <summary>
    ///     Prefab for a PQS
    /// </summary>
    public class PQSPreset
    {
        [ParserTarget("maxRadius")]
        public NumericParser<int> MaxRadius { get; set; }

        [ParserTarget("minRadius")]
        public NumericParser<int> MinRadius { get; set; }

        [ParserTarget("Mods")]
        public ConfigNode Mods { get; set; }
    }
}