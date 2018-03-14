﻿/*
Copyright (c) 2014, Lars Brubaker
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

using MatterControl.Printing;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MeshVisualizer;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;

namespace MatterHackers.MatterControl.ConfigurationPage.PrintLeveling
{
	public class ProbeCalibrationWizard : SystemWindow
	{
		protected WizardControl printLevelWizard;

		protected int totalSteps { get; private set; }
		protected PrinterConfig printer;

		private LevelingStrings levelingStrings;

		public ProbeCalibrationWizard(PrinterConfig printer)
			: base(500, 370)
		{
			this.printer = printer;
			AlwaysOnTopOfMain = true;
			this.totalSteps = 3;

			levelingStrings = new LevelingStrings(printer.Settings);
			string printLevelWizardTitle = ApplicationController.Instance.ProductName;
			string printLevelWizardTitleFull = "Probe Calibration Wizard".Localize();
			Title = string.Format("{0} - {1}", printLevelWizardTitle, printLevelWizardTitleFull);
			List<ProbePosition> autoProbePositions = new List<ProbePosition>(3);
			List<ProbePosition> manualProbePositions = new List<ProbePosition>(3);
			autoProbePositions.Add(new ProbePosition());
			manualProbePositions.Add(new ProbePosition());

			printLevelWizard = new WizardControl();
			AddChild(printLevelWizard);

			printLevelWizard.AddPage(new FirstPageInstructions(printer,
				"Probe Calibration Overview".Localize(), levelingStrings.CalibrateProbeWelcomText()));

			bool useZProbe = printer.Settings.Helpers.UseZProbe();
			printLevelWizard.AddPage(new HomePrinterPage(printer, printLevelWizard, 
				levelingStrings.HomingPageStepText, 
				levelingStrings.HomingPageInstructions(useZProbe),
				false));

			string positionLabel = "Position".Localize();
			string autoCalibrateLabel = "Auto Calibrate".Localize();
			string lowPrecisionLabel = "Low Precision".Localize();
			string medPrecisionLabel = "Medium Precision".Localize();
			string highPrecisionLabel = "High Precision".Localize();

			double startProbeHeight = printer.Settings.GetValue<double>(SettingsKey.print_leveling_probe_start);
			Vector2 probePosition = printer.Settings.GetValue<Vector2>(SettingsKey.print_center);

			int i = 0;
			// do the automatic probing of the center position
			var stepString = string.Format("{0} {1} {2} {3}:", levelingStrings.stepTextBeg, i + 1, levelingStrings.stepTextEnd, 3);
			printLevelWizard.AddPage(new AutoProbeFeedback(printer, printLevelWizard, new Vector3(probePosition, startProbeHeight), string.Format("{0} {1} {2} - {3}", stepString, positionLabel, i + 1, autoCalibrateLabel), autoProbePositions, i));

			// do the manual prob of the same position
			printLevelWizard.AddPage(new GetCoarseBedHeight(printer, printLevelWizard, new Vector3(probePosition, startProbeHeight), string.Format("{0} {1} {2} - {3}", levelingStrings.GetStepString(totalSteps), positionLabel, i + 1, lowPrecisionLabel), manualProbePositions, i, levelingStrings));
			printLevelWizard.AddPage(new GetFineBedHeight(printer, printLevelWizard, string.Format("{0} {1} {2} - {3}", levelingStrings.GetStepString(totalSteps), positionLabel, i + 1, medPrecisionLabel), manualProbePositions, i, levelingStrings));
			printLevelWizard.AddPage(new GetUltraFineBedHeight(printer, printLevelWizard, string.Format("{0} {1} {2} - {3}", levelingStrings.GetStepString(totalSteps), positionLabel, i + 1, highPrecisionLabel), manualProbePositions, i, levelingStrings));

			printLevelWizard.AddPage(new CalibrateProbeLastPagelInstructions(printer, printLevelWizard, 
				"Done".Localize(),
				"Your Probe is now calibrated.".Localize()  + "\n\n\t• " + "Remove the paper".Localize() + "\n\n" + "Click 'Done' to close this window.".Localize(), 
				autoProbePositions, 
				manualProbePositions));
		}

		private static SystemWindow probeCalibrationWizardWindow;

		public static void ShowProbeCalibrationWizard(PrinterConfig printer)
		{
			if (probeCalibrationWizardWindow == null)
			{
				bool levelingWasOn = printer.Settings.GetValue<bool>(SettingsKey.print_leveling_enabled);
				// turn off print leveling
				printer.Settings.Helpers.DoPrintLeveling(false);

				probeCalibrationWizardWindow = new ProbeCalibrationWizard(printer);

				probeCalibrationWizardWindow.ShowAsSystemWindow();

				probeCalibrationWizardWindow.Closed += (s, e) =>
				{
					// If leveling was on when we started, make sure it is on when we are done.
					if (levelingWasOn)
					{
						printer.Settings.Helpers.DoPrintLeveling(true);
					}

					probeCalibrationWizardWindow = null;

					// make sure we raise the probe on close 
					if (printer.Settings.GetValue<bool>(SettingsKey.has_z_probe)
						&& printer.Settings.GetValue<bool>(SettingsKey.use_z_probe)
						&& printer.Settings.GetValue<bool>(SettingsKey.has_z_servo))
					{
						// make sure the servo is retracted
						var servoRetract = printer.Settings.GetValue<double>(SettingsKey.z_servo_retracted_angle);
						printer.Connection.QueueLine($"M280 P0 S{servoRetract}");
					}
				};
			}
			else
			{
				probeCalibrationWizardWindow.BringToFront();
			}
		}
	}
}