﻿/**
 * Kopernicus Planetary System Modifier
 * ====================================
 * Created by: BryceSchroeder and Teknoman117 (aka. Nathaniel R. Lewis)
 * Maintained by: Thomas P., NathanKell and KillAshley
 * Additional Content by: Gravitasi, aftokino, KCreator, Padishar, Kragrathea, OvenProofMars, zengei, MrHappyFace
 * ------------------------------------------------------------- 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2015 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using ProceduralQuadSphere;

namespace Kopernicus
{
    namespace Configuration
    {
        namespace ModLoader
        {
            [RequireConfigType(ConfigType.Node)]
            public class FlattenAreaTangential : ModLoader<PQSMod_FlattenAreaTangential>
            {
                // flattenTo
                [ParserTarget("flattenTo")]
                public NumericParser<double> flattenTo
                {
                    get { return mod.flattenTo; }
                    set { mod.flattenTo = value; }
                }

                // innerRadius
                [ParserTarget("innerRadius")]
                public NumericParser<double> innerRadius
                {
                    get { return mod.innerRadius; }
                    set { mod.innerRadius = value; }
                }

                // outerRadius
                [ParserTarget("outerRadius")]
                public NumericParser<double> outerRadius
                {
                    get { return mod.outerRadius; }
                    set { mod.outerRadius = value; }
                }

                // position
                [ParserTarget("position")]
                public Vector3Parser position
                {
                    get { return mod.position; }
                    set { mod.position = value; }
                }

                // smoothEnd
                [ParserTarget("smoothEnd")]
                public NumericParser<double> smoothEnd
                {
                    get { return mod.smoothEnd; }
                    set { mod.smoothEnd = value; }
                }

                // smoothStart
                [ParserTarget("smoothStart")]
                public NumericParser<double> smoothStart
                {
                    get { return mod.smoothStart; }
                    set { mod.smoothStart = value; }
                }
            }
        }
    }
}