﻿/**
 * Stellarator - Creates procedural systems for Kopernicus
 * Copyright (c) 2016 Thomas P.
 * Licensed under the Terms of the MIT License
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Accrete;
using ConfigNodeParser;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using DynamicExpresso;
using Kopernicus.Configuration;
using Newtonsoft.Json.Linq;
using ProceduralQuadSphere;
using ProceduralQuadSphere.Unity;
using Stellarator.Database;
using Stellarator.JSON;
using Color = ProceduralQuadSphere.Unity.Color;
using XnaGeometry;

namespace Stellarator
{
    public class Generator
    {
        /// <summary>
        /// The random number generator
        /// </summary>
        public static Random Random { get; set; }

        /// <summary>
        /// The Kerbin equivalent for the system
        /// </summary>
        public static Planet Kerbin { get; set; }

        /// <summary>
        /// All Stock bodies that we can use as a template
        /// </summary>
        public static String[] Templates = {"Moho", "Eve", "Gilly", "Mun", "Minmus", "Duna", "Ike", "Dres", "Laythe", "Vall", "Tylo", "Bop", "Pol", "Eeloo"};

        /// <summary>
        /// Files that should get copied
        /// </summary>
        public static Dictionary<String, String> toCopy = new Dictionary<String, String>();

        /// <summary>
        /// A header that is added to each config file
        /// </summary>
        public const String header = "// Generated by Stellarator (https://github.com/Kopernicus/Stellarator)\n// License: MIT (Configs and Code); All Rights Reserved (Art Assets)\n// System Seed: {0}\n\n";

        /// <summary>
        /// This is the core of the whole app. It generates a solar system, based on a seed.
        /// </summary>
        public static void Generate(String seed, String folder, Boolean systematicNames)
        {
            // Log
            Console.WriteLine("Generating the solar system...");
            Console.WriteLine();

            // Create a new Solar System using libaccrete
            SolarSystem system = new SolarSystem(false, true, s => { });
            SolarSystem.Generate(ref system, seed.GetHashCode(), Int32.MaxValue);

            // Reuse the random component
            Random = system.random;

            // All Nodes
            List<ConfigNode> nodes = new List<ConfigNode>();

            // Create the root node
            ConfigNode root = new ConfigNode("@Kopernicus:AFTER[KOPERNICUS]");
            ConfigNode deletor = new ConfigNode("!Body,*");
            root.AddConfigNode(deletor);

            // Save the config
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/systems/" + folder);
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/systems/" + folder + "/Configs");
            ConfigNode wrapper = new ConfigNode();
            wrapper.AddConfigNode(root);
            wrapper.Save(Directory.GetCurrentDirectory() + "/systems/" + folder + "/System.cfg", String.Format(header, seed));

            // Sun
            nodes.Add(GenerateSun(system, folder));

            // Select Kerbin
            List<Planet> allBodies = new List<Planet>(system.bodies);
            allBodies = allBodies.Where(p => !p.gas_giant && p.surface_pressure > 0).ToList();
            Kerbin = allBodies[Random.Next(0, allBodies.Count)];

            // Iterate over all bodies in the generated system
            Int32 pCount = 1;
            foreach (Planet planet in system.bodies)
            {
                ConfigNode node = GenerateBody(planet, folder, systematicName: systematicNames ? nodes[0].GetValue("cbNameLater") + "-" + pCount : null);
                nodes.Add(node);
                Int32 mCount = 1;
                foreach (Planet moon in planet.bodies_orbiting)
                {
                    String name = node.HasValue("cbNameLater") ? node.GetValue("cbNameLater") : node.GetValue("name");
                    nodes.Add(GenerateBody(moon, folder, node.GetValue("name"), systematicNames ? name + "-" + mCount : null));
                    mCount++;
                }
                pCount++;
            }

            // Log
            Console.WriteLine("Saving the system");

            // Save the config
            foreach (ConfigNode n in nodes)
            {
                root = new ConfigNode("@Kopernicus:AFTER[KOPERNICUS]");
                root.AddConfigNode(n);
                wrapper = new ConfigNode();
                wrapper.AddConfigNode(root);
                String name = n.HasValue("cbNameLater") ? n.GetValue("cbNameLater") : n.GetValue("name");
                wrapper.Save(Directory.GetCurrentDirectory() + "/systems/" + folder + "/Configs/" + name + ".cfg", String.Format(header, seed));            
                
                // Log
                Console.WriteLine($"Saved {name}");
            }

            // Copy files
            foreach (KeyValuePair<String, String> kvP in toCopy)
                File.Copy(kvP.Key, kvP.Value);
        }

        /// <summary>
        /// Creates the ConfigNode for the SunBody
        /// </summary>
        public static ConfigNode GenerateSun(SolarSystem system, String folder)
        {
            // Create the Body node
            String name = Utility.GenerateName();
            ConfigNode node = new ConfigNode("Body");
            node.AddValue("name", "Sun");
            node.AddValue("cbNameLater", name);

            // Template
            ConfigNode template = new ConfigNode("Template");
            node.AddConfigNode(template);
            template.AddValue("name", "Sun");

            // Properties
            ConfigNode properties = new ConfigNode("Properties");
            node.AddConfigNode(properties);
            properties.AddValue("radius", "" + system.stellar_radius_ratio * 261600000);
            properties.AddValue("mass", "" + system.stellar_mass_ratio * 1.75656696858329E+28);
            // TODO: Timewarplimits
            properties.AddValue("useTheInName", "False");

            // Log
            Console.WriteLine($"Generated root body named {name}");

            // Scaled Space
            ConfigNode scaled = new ConfigNode("ScaledVersion");
            node.AddConfigNode(scaled);

            // Load database stuff
            Dictionary<String, StarPrefab[]> data = Utility.Load<Dictionary<String, StarPrefab[]>>("stars.json");
            StarPrefab star = data[system.type.star_class][Random.Next(0, data[system.type.star_class].Length)];

            // Materials
            ConfigNode mat = new ConfigNode("Material");
            scaled.AddConfigNode(mat);
            mat.AddValue("emitColor0", Parser.WriteColor(star.material.emitColor0));
            mat.AddValue("emitColor1", Parser.WriteColor(star.material.emitColor1));
            mat.AddValue("sunspotPower", "" + star.material.sunspotPower);
            mat.AddValue("sunspotColor", Parser.WriteColor(star.material.sunspotColor));
            mat.AddValue("rimColor", Parser.WriteColor(star.material.rimColor));
            mat.AddValue("rimPower", "" + star.material.rimPower);
            mat.AddValue("rimBlend", "" + star.material.rimBlend);

            // Light Node
            ConfigNode light = new ConfigNode("Light");
            scaled.AddConfigNode(light);
            light.AddValue("sunlightColor", Parser.WriteColor(star.light.sunlightColor));
            light.AddValue("sunlightIntensity", "" + star.light.sunlightIntensity);
            light.AddValue("sunlightShadowStrength", "" + star.light.sunlightShadowStrength);
            light.AddValue("scaledSunlightColor", Parser.WriteColor(star.light.scaledSunlightColor)); 
            light.AddValue("scaledSunlightIntensity", "" + star.light.scaledSunlightIntensity);
            light.AddValue("IVASunColor", Parser.WriteColor(star.light.IVASunColor));
            light.AddValue("IVASunIntensity", "" + star.light.IVASunIntensity);
            light.AddValue("ambientLightColor", Parser.WriteColor(star.light.ambientLightColor));
            light.AddValue("sunLensFlareColor", Parser.WriteColor(star.light.sunLensFlareColor)); 
            light.AddValue("givesOffLight", "" + star.light.givesOffLight);
            light.AddValue("luminosity", "" + system.stellar_luminosity_ratio * 1360);

            // TODO: Coronas       

            // Log
            Console.WriteLine($"Generated scaled space for {name}");
            Console.WriteLine();

            // Return it
            return node;
        }

        /// <summary>
        /// Generates the parameters for a planet
        /// </summary>
        public static ConfigNode GenerateBody(Planet planet, String folder, String referenceBody = "Sun", String systematicName = null)
        {
            String name = systematicName ?? Utility.GenerateName();
            ConfigNode node = new ConfigNode("Body");

            // Name
            if (planet != Kerbin)
                node.AddValue("name", name);
            else
            {
                node.AddValue("name", "Kerbin");
                node.AddValue("cbNameLater", name);
            }

            // Random Main Menu Body
            node.AddValue("randomMainMenuBody", "" + (referenceBody == "Sun"));

            // Template
            ConfigNode template = new ConfigNode("Template");
            node.AddConfigNode(template);
            if (planet != Kerbin)
            {
                if (planet.gas_giant)
                    template.AddValue("name", "Jool");
                else
                {
                    template.AddValue("name", Utility.GetTemplate(planet.surface_pressure > 0.00001, false));
                    template.AddValue("removeAllPQSMods", "True");
                    if (planet.surface_pressure <= 0.00001)
                        template.AddValue("removeAtmosphere", "True");
                    template.AddValue("removeOcean", "True");
                    // TODO: Handle atmospheres and ocean
                }
            }
            else
            {
                template.AddValue("name", "Kerbin");
                template.AddValue("removePQSMods", "VertexSimplexHeightAbsolute, VertexHeightNoiseVertHeightCurve2, VertexRidgedAltitudeCurve, LandControl, AerialPerspectiveMaterial, VertexHeightMap, QuadEnhanceCoast, MapDecalTangent, MapDecal, FlattenArea[IslandAirfield], PQSCity[KSC2], PQSCity[IslandAirfield]");
                template.AddValue("removeAtmosphere", "True");
                template.AddValue("removeOcean", "True");
            }

            // Properties
            ConfigNode properties = new ConfigNode("Properties");
            node.AddConfigNode(properties);
            properties.AddValue("radius", "" + planet.radius * 100);
            properties.AddValue("geeASL", "" + planet.surface_grav);
            properties.AddValue("rotationPeriod", "" + planet.day * 60 * 60);
            properties.AddValue("tidallyLocked", "" + planet.resonant_period);
            properties.AddValue("initialRotation", "" + Random.Next(0, 361));
            properties.AddValue("albedo", "" + planet.albedo);
            // TODO: Timewarplimits
            properties.AddValue("useTheInName", "False");

            // Log
            Console.WriteLine($"Generated a planet named {name}. GasGiant: {planet.gas_giant}. Template: {template.GetValue("name")}");

            // Color
            Color planetColor = Utility.GenerateColor();

            // Orbit
            ConfigNode orbit = new ConfigNode("Orbit");
            node.AddConfigNode(orbit);
            orbit.AddValue("referenceBody", referenceBody);
            orbit.AddValue("eccentricity", "" + planet.e);
            orbit.AddValue("semiMajorAxis", "" + planet.a * 100 * Constants.KM_PER_AU);
            orbit.AddValue("longitudeOfAscendingNode", "" + Random.Range(0, 181));
            orbit.AddValue("meanAnomalyAtEpochD", "" + Random.Range(0, 181));
            if (planet.gas_giant)
                orbit.AddValue("color", Parser.WriteColor(Utility.AlterColor(planetColor)));
            // Inclination
            Double[] data = Utility.Load<Double[]>("inclination.json");
            Double v = data[Random.Next(data.Length)];
            orbit.AddValue("inclination", "" + v);

            // Log
            Console.WriteLine($"Generated orbit around {referenceBody} for {name}");

            // Scaled Space
            ConfigNode scaled = new ConfigNode("ScaledVersion");
            node.AddConfigNode(scaled);

            // Material
            ConfigNode mat = new ConfigNode("Material");
            scaled.AddConfigNode(mat);
            if (!planet.gas_giant)
            {
                mat.AddValue("texture", folder + "/PluginData/" + name + "_Texture.png");
                mat.AddValue("normals", folder + "/PluginData/" + name + "_Normals.png");
            }
            if (planet.gas_giant)
            {
                // Texture
                String[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/data/textures/");
                String texture = files[Random.Next(0, files.Length)];
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/");
                File.Copy(texture, Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/" + name + "_Texture.png", true);
                mat.AddValue("texture", folder + "/PluginData/" + name + "_Texture.png");

                // Load the Image
                Color average;
                using (Bitmap image = new Bitmap(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/" + name + "_Texture.png"))
                    average = Utility.GetAverageColor(image);

                // Scale
                mat.AddValue("mainTexScale", (Random.Next(0, 2) ==1 ? 1 : -1) + "," + (Random.Next(0, 2) == 1 ? 1 : -1));

                mat.AddValue("color", Parser.WriteColor(Utility.ReColor(Utility.AlterColor(planetColor), average)));
                ConfigNode gradient = new ConfigNode("Gradient");
                mat.AddConfigNode(gradient);
                gradient.AddValue("0.0", Parser.WriteColor(Utility.AlterColor(planetColor)));
                gradient.AddValue("0.6", "0.0549,0.0784,0.141,1");
                gradient.AddValue("1.0", "0.0196,0.0196,0.0196,1");
            }

            // Log
            Console.WriteLine($"Generated scaled space for {name}");

            // Atmosphere
            if (planet.surface_pressure > 0.00001)
            {
                ConfigNode atmosphere = new ConfigNode("Atmosphere");
                node.AddConfigNode(atmosphere);
                atmosphere.AddValue("enabled", "True");
                atmosphere.AddValue("oxygen", "" + Random.Boolean(10));
                atmosphere.AddValue("atmosphereDepth", "" + planet.radius * 100 * Random.Range(0.1, 0.16));
                atmosphere.AddValue("atmosphereMolarMass", "" + planet.molecule_weight / 1000);
                atmosphere.AddValue("staticPressureASL", "" + planet.surface_pressure / 1000 * 101.324996948242);
                atmosphere.AddValue("temperatureSeaLevel", "" + planet.surface_temp);
                if (planet.gas_giant)
                {
                    atmosphere.AddValue("ambientColor", Parser.WriteColor(Utility.AlterColor(planetColor)));
                    atmosphere.AddValue("lightColor", Parser.WriteColor(Utility.AlterColor(Utility.LightColor(planetColor))));
                }
                GenerateAtmosphereCurves(ref atmosphere, planet.gas_giant ? "Jool" : Utility.GetTemplate(true, true));            
                
                // Log
                Console.WriteLine($"Generated atmosphere for {name}");
            }

            // Rings :D
            if (planet.gas_giant && Random.Next(0, 100) < 5)
            {
                ConfigNode rings = new ConfigNode("Rings");
                node.AddConfigNode(rings);
                List<RingPrefab[]> definitions = Utility.Load<List<RingPrefab[]>>("rings.json");
                RingPrefab[] def = definitions[Random.Next(0, definitions.Count)];
                foreach (RingPrefab r in def)
                {
                    ConfigNode ring = new ConfigNode("Ring");
                    rings.AddConfigNode(ring);
                    ring.AddValue("innerRadius", "" + planet.radius * 0.1 * r.innerRadius);
                    ring.AddValue("outerRadius", "" + planet.radius * 0.1 * r.outerRadius);
                    ring.AddValue("angle", "" + r.angle);
                    ring.AddValue("color", Parser.WriteColor(Utility.AlterColor(planetColor)));
                    ring.AddValue("lockRotation", "" + r.lockRotation);
                    ring.AddValue("unlit", "False");
                }

                // Log
                Console.WriteLine($"Generated rings around {name}");
            }

            // PQS
            if (!planet.gas_giant)
            {
                Color average = default(Color);
                GeneratePQS(ref node, name, folder, planet, ref average);

                // Apply colors
                orbit.AddValue("color", Parser.WriteColor(Utility.AlterColor(average)));
                if (node.HasNode("Atmosphere"))
                {
                    ConfigNode atmosphere = node.GetNode("Atmosphere");
                    atmosphere.AddValue("ambientColor", Parser.WriteColor(Utility.AlterColor(average)));
                    atmosphere.AddValue("lightColor", Parser.WriteColor(Utility.AlterColor(Utility.LightColor(average))));
                }
                ConfigNode gradient = new ConfigNode("Gradient");
                mat.AddConfigNode(gradient);
                gradient.AddValue("0.0", Parser.WriteColor(Utility.AlterColor(average)));
                gradient.AddValue("0.6", "0.0549,0.0784,0.141,1");
                gradient.AddValue("1.0", "0.0196,0.0196,0.0196,1");
            }

            // Log
            Console.WriteLine($"Generation of body {name} finished!");
            Console.WriteLine();

            // Return
            return node;
        }

        /// <summary>
        /// Adds static atmosphere curves
        /// </summary>
        /// <param name="atmosphere"></param>
        public static void GenerateAtmosphereCurves(ref ConfigNode atmosphere, String template)
        {
            ConfigNode pressure = ConfigNode.Load(Directory.GetCurrentDirectory() + "/data/curves/" + template + "Pressure.cfg");
            pressure.name = "pressureCurve";
            atmosphere.AddConfigNode(pressure);
            atmosphere.AddValue("pressureCurveIsNormalized", "True");
            ConfigNode temperature = ConfigNode.Load(Directory.GetCurrentDirectory() + "/data/curves/" + template + "Temperature.cfg");
            temperature.name = "temperatureCurve";
            atmosphere.AddConfigNode(temperature);
            atmosphere.AddValue("temperatureCurveIsNormalized", "True");
        }

        /// <summary>
        /// Generates a PQS Setup + the Scaled Space Maps needed
        /// </summary>
        /// <returns></returns>
        public static void GeneratePQS(ref ConfigNode node, String name, String folder, Planet planet, ref Color average)
        {                
            // Log
            Console.WriteLine("Preparing to load PQS data");

            // Create the node
            ConfigNode pqs = new ConfigNode("PQS");

            // TODO: Material Settings?

            // Create a node for the mods
            ConfigNode mods = new ConfigNode("Mods");
            pqs.AddConfigNode(mods);

            // Load the PQSDatabase and select a setup
            List<PQSPreset> data = Utility.Load<List<PQSPreset>>("pqs.json");
            PQSPreset setup = data.Where(d => planet.radius * 100 > d.minRadius && planet.radius * 100 < d.maxRadius).ToArray()[Random.Next(0, data.Count)];

            // Setup the interpreter
            Interpreter interpreter = new Interpreter().SetVariable("planet", planet, typeof(Planet)).SetVariable("pqsVersion", setup, typeof(PQSPreset));

            // Convert the JSON objects to a config node
            foreach (var kvP in setup.mods)
            {
                // Create the Mod node
                ConfigNode mod = new ConfigNode(kvP.Key);
                mods.AddConfigNode(mod);

                // Process the values
                foreach (var moddata in kvP.Value)
                {
                    JToken tmp;
                    JObject jObject = moddata.Value as JObject;
                    if (jObject == null)
                        mod.AddValue(moddata.Key, interpreter.TryEval(moddata.Value.ToString()));
                    else if (jObject.TryGetValue("min", out tmp) && jObject.TryGetValue("max", out tmp))
                        mod.AddValue(moddata.Key, "" + Range.Converter.Create(jObject, interpreter));
                    else
                        mod.AddConfigNode(Utility.JSONToNode(moddata.Key, jObject, interpreter));
                }
            }

            // Create a new PQSObject
            PQS pqsVersion = new PQS(planet.radius * 100);
            List<PQSMod> patchedMods = new List<PQSMod>(); 
            
            // Log
            Console.WriteLine($"Created PQS Object for {name}");

            // Get all loaded types
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToList();

            // Load mods from Config
            foreach (ConfigNode mod in mods.nodes)
            {
                // get the mod type
                if (types.Count(t => t.Name == mod.name) == 0)
                    continue;
                Type loaderType = types.FirstOrDefault(t => t.Name == mod.name);
                String testName = mod.name != "LandControl" ? "PQSMod_" + mod.name : "PQSLandControl";
                Type modType = types.FirstOrDefault(t => t.Name == testName);
                if (loaderType == null || modType == null)
                    continue;

                // Do any PQS Mods already exist on this PQS matching this mod?
                IEnumerable<PQSMod> existingMods = pqsVersion.mods.Where(m => m.GetType() == modType);

                // Create the loader
                Object loader = Activator.CreateInstance(loaderType);

                // Reflection, because C# being silly... :/
                MethodInfo createNew = loaderType.GetMethod("Create", new Type[] { typeof(PQS) });
                MethodInfo create = loaderType.GetMethod("Create", new Type[] { modType });

                if (existingMods.Any())
                {
                    // Attempt to find a PQS mod we can edit that we have not edited before
                    PQSMod existingMod = existingMods.FirstOrDefault(m => !patchedMods.Contains(m) && (!mod.HasValue("name") || (mod.HasValue("index") ? existingMods.ToList().IndexOf(m) == Int32.Parse(mod.GetValue("index")) && m.name == mod.GetValue("name") : m.name == mod.GetValue("name"))));
                    if (existingMod != null)
                    {
                        create.Invoke(loader, new[] { existingMod });
                        Parser.LoadObjectFromConfigurationNode(loader, mod);
                        patchedMods.Add(existingMod);
                    }
                    else
                    {
                        createNew.Invoke(loader, new [] { pqsVersion });
                        Parser.LoadObjectFromConfigurationNode(loader, mod);
                    }
                }
                else
                {
                    createNew.Invoke(loader, new[] { pqsVersion });
                    Parser.LoadObjectFromConfigurationNode(loader, mod);
                }

                // Log
                Console.WriteLine($"Created a new instance of {loaderType.Name} on {name}");
            }            
            
            // Size
            Int32 width = pqsVersion.radius >= 600000 ? 4096 : pqsVersion.radius <= 100000 ? 1024 : 2048;

            // Export ScaledSpace Maps
            Bitmap diffuse = new Bitmap(width, width / 2);
            Bitmap height  = new Bitmap(width, width / 2);                
            
            // Log
            Console.WriteLine($"Exporting Scaled Space maps from the PQS. This could take a while...");

            // Iterate over the PQS
            for (Int32 i = 0; i < width; i++)
            {
                for (Int32 j = 0; j < width / 2; j++)
                {
                    // Create a VertexBuildData
                    VertexBuildData builddata = new VertexBuildData
                    {
                        directionFromCenter = Quaternion.CreateFromAxisAngle(Vector3.Up, 360d / 2048 * i) * Quaternion.CreateFromAxisAngle(Vector3.Right, 90d - 180d / (width / 2) * j) * Vector3.Forward,
                        vertHeight = pqsVersion.radius
                    };

                    // Build the maps
                    foreach (PQSMod m in pqsVersion.mods)
                        m.OnVertexBuildHeight(builddata);
                    foreach (PQSMod m in pqsVersion.mods) // Memory? Who cares
                        m.OnVertexBuild(builddata);

                    Double h = (builddata.vertHeight - pqsVersion.radius);
                    if (h < 0)
                        h = 0;
                    Color c = builddata.vertColor;
                    c.a = 1f;
                    diffuse.SetPixel(i, j, c);
                    height.SetPixel(i, j, new Color((Single) h, (Single) h, (Single) h, 1f));
                }
            }

            // Save the textures
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/");
            diffuse.Save(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/" + name + "_Texture.png", ImageFormat.Png);
            height.Save(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/" + name + "_Height.png", ImageFormat.Png); // In case you need it :)
            Bitmap normals = Utility.BumpToNormalMap(height, 7); // TODO: Implement something to make strength dynamic
            normals.Save(Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/" + name + "_Normals.png", ImageFormat.Png);

            // Log
            Console.WriteLine($"Saved maps to {Directory.GetCurrentDirectory() + "/systems/" + folder + "/PluginData/"}");

            // Finish
            node.AddConfigNode(pqs);

            // Colors
            average = Utility.GetAverageColor(diffuse);
        }
    }
}