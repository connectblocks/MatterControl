﻿/*
Copyright (c) 2016, Lars Brubaker
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
using System.Collections.Generic;
using System.Linq;

using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ActionBar
{
	internal class PrintActionRow : FlowLayoutWidget
	{
		private List<Button> activePrintButtons = new List<Button>();
		private List<Button> allPrintButtons = new List<Button>();

		private Button cancelConnectButton;
		private Button resetConnectionButton;
		private Button resumeResumeButton;

		private Button startPrintButton;
		private Button pausePrintButton;
		private Button cancelPrintButton;

		private Button finishSetupButton;

		private EventHandler unregisterEvents;

		private PrinterConfig printer;

		public PrintActionRow(PrinterConfig printer, ThemeConfig theme, GuiWidget parentWidget)
		{
			this.printer = printer;

			this.HAnchor = HAnchor.Stretch;

			AddChildElements(theme.ButtonFactory, parentWidget, theme.ButtonSpacing);

			// Add Handlers
			printer.Connection.CommunicationStateChanged.RegisterEvent(onStateChanged, ref unregisterEvents);
			ProfileManager.ProfilesListChanged.RegisterEvent(onStateChanged, ref unregisterEvents);
		}

		protected void AddChildElements(TextImageButtonFactory buttonFactory, GuiWidget parentWidget, BorderDouble defaultMargin)
		{
			startPrintButton = buttonFactory.Generate("Print".Localize().ToUpper());
			startPrintButton.Name = "Start Print Button";
			startPrintButton.ToolTipText = "Begin printing the selected item.".Localize();
			startPrintButton.Margin = defaultMargin;
			startPrintButton.Click += onStartButton_Click;

			finishSetupButton = buttonFactory.Generate("Finish Setup...".Localize());
			finishSetupButton.Name = "Finish Setup Button";
			finishSetupButton.ToolTipText = "Run setup configuration for printer.".Localize();
			finishSetupButton.Margin = defaultMargin;
			finishSetupButton.Click += onStartButton_Click;

			resetConnectionButton = buttonFactory.Generate("Reset".Localize().ToUpper(), AggContext.StaticData.LoadIcon("e_stop.png", 14, 14, IconColor.Theme));
			resetConnectionButton.ToolTipText = "Reboots the firmware on the controller".Localize();
			resetConnectionButton.Margin = defaultMargin;
			resetConnectionButton.Click += (s, e) => UiThread.RunOnIdle(printer.Connection.RebootBoard);

			pausePrintButton = buttonFactory.Generate("Pause".Localize().ToUpper());
			pausePrintButton.ToolTipText = "Pause the current print".Localize();
			pausePrintButton.Margin = defaultMargin;
			pausePrintButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(printer.Connection.RequestPause);
				pausePrintButton.Enabled = false;
			};
			parentWidget.AddChild(pausePrintButton);
			allPrintButtons.Add(pausePrintButton);

			cancelConnectButton = buttonFactory.Generate("Cancel Connect".Localize().ToUpper());
			cancelConnectButton.ToolTipText = "Stop trying to connect to the printer.".Localize();
			cancelConnectButton.Margin = defaultMargin;
			cancelConnectButton.Click += (s, e) => UiThread.RunOnIdle(() =>
			{
				ApplicationController.Instance.ConditionalCancelPrint();
				UiThread.RunOnIdle(SetButtonStates);
			});

			cancelPrintButton = buttonFactory.Generate("Cancel".Localize().ToUpper());
			cancelPrintButton.ToolTipText = "Stop the current print".Localize();
			cancelPrintButton.Name = "Cancel Print Button";
			cancelPrintButton.Margin = defaultMargin;
			cancelPrintButton.Click += (s, e) => UiThread.RunOnIdle(() =>
			{
				ApplicationController.Instance.ConditionalCancelPrint();
				SetButtonStates();
			});

			resumeResumeButton = buttonFactory.Generate("Resume".Localize().ToUpper());
			resumeResumeButton.ToolTipText = "Resume the current print".Localize();
			resumeResumeButton.Margin = defaultMargin;
			resumeResumeButton.Name = "Resume Button";
			resumeResumeButton.Click += (s, e) =>
			{
				if (printer.Connection.PrinterIsPaused)
				{
					printer.Connection.Resume();
				}
				pausePrintButton.Enabled = true;
			};

			parentWidget.AddChild(resumeResumeButton);
			allPrintButtons.Add(resumeResumeButton);
			this.Margin = 0;
			this.HAnchor = HAnchor.Fit;

			parentWidget.AddChild(startPrintButton);
			allPrintButtons.Add(startPrintButton);

			parentWidget.AddChild(finishSetupButton);
			allPrintButtons.Add(finishSetupButton);

			parentWidget.AddChild(cancelPrintButton);
			allPrintButtons.Add(cancelPrintButton);

			parentWidget.AddChild(cancelConnectButton);
			allPrintButtons.Add(cancelConnectButton);

			parentWidget.AddChild(resetConnectionButton);
			allPrintButtons.Add(resetConnectionButton);

			SetButtonStates();

			printer.Settings.PrintLevelingEnabledChanged.RegisterEvent((s, e) => SetButtonStates(), ref unregisterEvents);
		}

		protected void DisableActiveButtons()
		{
			foreach (Button button in this.activePrintButtons)
			{
				button.Enabled = false;
			}
		}

		protected void EnableActiveButtons()
		{
			foreach (Button button in this.activePrintButtons)
			{
				button.Enabled = true;
			}
		}

		//Set the states of the buttons based on the status of PrinterCommunication
		protected void SetButtonStates()
		{
			this.activePrintButtons.Clear();
			if (!printer.Connection.PrinterIsConnected
				&& printer.Connection.CommunicationState != CommunicationStates.AttemptingToConnect)
			{
				if (!ProfileManager.Instance.ActiveProfiles.Any())
				{
					// TODO: Possibly upsell add printer - ideally don't show printer tab, only show Plus tab
					//this.activePrintButtons.Add(addPrinterButton);
				}

				ShowActiveButtons();
				EnableActiveButtons();
			}
			else
			{
				switch (printer.Connection.CommunicationState)
				{
					case CommunicationStates.AttemptingToConnect:
						this.activePrintButtons.Add(cancelConnectButton);
						EnableActiveButtons();
						break;

					case CommunicationStates.Connected:
						PrintLevelingData levelingData = printer.Settings.Helpers.GetPrintLevelingData();
						if (levelingData != null && printer.Settings.GetValue<bool>(SettingsKey.print_leveling_required_to_print)
							&& !levelingData.HasBeenRunAndEnabled())
						{
							this.activePrintButtons.Add(finishSetupButton);
						}
						else
						{
							this.activePrintButtons.Add(startPrintButton);
						}

						EnableActiveButtons();
						break;

					case CommunicationStates.PreparingToPrint:
						this.activePrintButtons.Add(cancelPrintButton);
						EnableActiveButtons();
						break;

					case CommunicationStates.PrintingFromSd:
					case CommunicationStates.Printing:
						if (!printer.Connection.PrintWasCanceled)
						{
							this.activePrintButtons.Add(pausePrintButton);
							this.activePrintButtons.Add(cancelPrintButton);
						}
						else if (UserSettings.Instance.IsTouchScreen)
						{
							this.activePrintButtons.Add(resetConnectionButton);
						}

						EnableActiveButtons();
						break;

					case CommunicationStates.Paused:
						this.activePrintButtons.Add(resumeResumeButton);
						this.activePrintButtons.Add(cancelPrintButton);
						EnableActiveButtons();
						break;

					case CommunicationStates.FinishedPrint:
						this.activePrintButtons.Add(startPrintButton);
						EnableActiveButtons();
						break;

					default:
						DisableActiveButtons();
						break;
				}
			}

			if (printer.Connection.PrinterIsConnected
				&& printer.Settings.GetValue<bool>(SettingsKey.show_reset_connection))
			{
				this.activePrintButtons.Add(resetConnectionButton);
				ShowActiveButtons();
				EnableActiveButtons();
			}
			ShowActiveButtons();
		}

		protected void ShowActiveButtons()
		{
			foreach (Button button in this.allPrintButtons)
			{
				if (activePrintButtons.IndexOf(button) >= 0)
				{
					button.Visible = true;
				}
				else
				{
					button.Visible = false;
				}
			}
		}

		private void onStartButton_Click(object sender, EventArgs mouseEvent)
		{
			UiThread.RunOnIdle(() =>
			{
				ApplicationController.Instance.PrintActivePartIfPossible(printer.Bed.printItem);
			});
		}

		private void onStateChanged(object sender, EventArgs e)
		{
			UiThread.RunOnIdle(SetButtonStates);
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}
	}
}