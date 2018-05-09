﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using MatterControl.Printing;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public class GCodeDetails
	{
		private GCodeFile loadedGCode;

		private PrinterConfig printer;
		public GCodeDetails(PrinterConfig printer, GCodeFile loadedGCode)
		{
			this.printer = printer;
			this.loadedGCode = loadedGCode;
		}

		public string SecondsToTime(double seconds)
		{
			int secondsRemaining = (int)seconds;
			int hoursRemaining = (int)(secondsRemaining / (60 * 60));
			int minutesRemaining = (int)((secondsRemaining + 30) / 60 - hoursRemaining * 60); // +30 for rounding

			secondsRemaining = secondsRemaining % 60;

			if (hoursRemaining > 0)
			{
				return $"{hoursRemaining} h, {minutesRemaining} min";
			}
			else if(minutesRemaining > 10)
			{
				return $"{minutesRemaining} min";
			}
			else
			{
				return $"{minutesRemaining} min {secondsRemaining} s";
			}
		}

		public string EstimatedPrintTime
		{
			get
			{
				if (loadedGCode == null)
				{
					return "---";
				}

				return SecondsToTime(loadedGCode.Instruction(0).secondsToEndFromHere);
			}
		}

		public string FilamentUsed => string.Format("{0:0.0} mm", loadedGCode.GetFilamentUsedMm(printer.Settings.GetValue<double>(SettingsKey.filament_diameter)));

		public string FilamentVolume => string.Format("{0:0.00} cm³", loadedGCode.GetFilamentCubicMm(printer.Settings.GetValue<double>(SettingsKey.filament_diameter)) / 1000);

		public string EstimatedMass => this.TotalMass <= 0 ? "Unknown" : string.Format("{0:0.00} g", this.TotalMass);

		public string EstimatedCost => this.TotalCost <= 0 ? "Unknown" : string.Format("${0:0.00}", this.TotalCost);

		public double TotalMass
		{
			get
			{
				double filamentDiameter = printer.Settings.GetValue<double>(SettingsKey.filament_diameter);
				double filamentDensity = printer.Settings.GetValue<double>(SettingsKey.filament_density);

				return loadedGCode.GetFilamentWeightGrams(filamentDiameter, filamentDensity);
			}
		}

		public double GetLayerHeight(int layerIndex)
		{
			return loadedGCode.GetLayerHeight(layerIndex);
		}

		internal object GetLayerZOffset(int layerIndex)
		{
			return loadedGCode.GetLayerZOffset(layerIndex);
		}

		public string LayerTime(int activeLayerIndex)
		{
			if (loadedGCode == null)
			{
				return "---";
			}

			int startInstruction = Math.Min(loadedGCode.LayerCount - 1, loadedGCode.GetInstructionIndexAtLayer(activeLayerIndex));
			int endInstruction = loadedGCode.GetInstructionIndexAtLayer(activeLayerIndex + 1);
			var secondsToEndFromStart = loadedGCode.Instruction(startInstruction).secondsToEndFromHere;
			var secondsToEndFromEnd = loadedGCode.Instruction(endInstruction).secondsToEndFromHere;
			return SecondsToTime(secondsToEndFromStart - secondsToEndFromEnd);
		}

		internal string GetLayerFanSpeeds(int activeLayerIndex)
		{
			if (loadedGCode == null)
			{
				return "---";
			}

			int startInstruction = loadedGCode.GetInstructionIndexAtLayer(activeLayerIndex);
			if(activeLayerIndex == 0)
			{
				startInstruction = 0;
			}
			int endInstruction = loadedGCode.GetInstructionIndexAtLayer(activeLayerIndex + 1);

			string separator = "";
			string fanSpeeds = "";
			for (int i = startInstruction; i < endInstruction; i++)
			{
				var line = loadedGCode.Instruction(i).Line;
				if (line.StartsWith("M107")) // fan off
				{
					fanSpeeds += separator + "Off";
					separator = ", ";
				}
				else if(line.StartsWith("M106")) // fan on
				{
					double speed = 0;
					if (GCodeFile.GetFirstNumberAfter("M106", line, ref speed, 0, ""))
					{
						fanSpeeds += separator + $"{speed/255*100:0}%";
						separator = ", ";
					}
				}
			}

			return fanSpeeds;
		}

		public double TotalCost
		{
			get
			{
				double filamentCost = printer.Settings.GetValue<double>(SettingsKey.filament_cost);
				return this.TotalMass / 1000 * filamentCost;
			}
		}
	}
}