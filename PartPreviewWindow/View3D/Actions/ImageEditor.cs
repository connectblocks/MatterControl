﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
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
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PartPreviewWindow;

namespace MatterHackers.MatterControl.DesignTools
{
	using CustomWidgets;
	using DataConverters3D;
	using MatterHackers.Agg.Platform;
	using MatterHackers.MatterControl.Library;

	public class ImageEditor : IObject3DEditor
	{
		bool IObject3DEditor.Unlocked => true;

		string IObject3DEditor.Name => "Image Editor";

		IEnumerable<Type> IObject3DEditor.SupportedTypes() => new[] { typeof(ImageObject3D) };

		public GuiWidget Create(IObject3D item, ThemeConfig theme)
		{
			var column = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.MaxFitOrStretch
			};

			var imageObject = item as ImageObject3D;

			var activeImage = imageObject.Image;

			var imageSection = new SectionWidget(
				"Image".Localize(),
				new FlowLayoutWidget(FlowDirection.TopToBottom),
				theme);

			theme.ApplyBoxStyle(imageSection, margin: 0);

			column.AddChild(imageSection);

			ImageBuffer thumbnailImage = SetImage(theme, imageObject);

			ImageWidget thumbnailWidget;
			imageSection.ContentPanel.AddChild(thumbnailWidget = new ImageWidget(thumbnailImage)
			{
				Margin = new BorderDouble(bottom: 5),
				HAnchor = HAnchor.Center
			});

			bool addImageSearch = true;
			if(addImageSearch)
			{
				// add a search Google box
				var searchRow = new FlowLayoutWidget()
				{
					HAnchor = HAnchor.Stretch,
					VAnchor = VAnchor.Fit,
				};
				imageSection.ContentPanel.AddChild(searchRow);

				MHTextEditWidget textToAddWidget = new MHTextEditWidget("", pixelWidth: 300, messageWhenEmptyAndNotSelected: "Search Google".Localize());
				textToAddWidget.HAnchor = HAnchor.Stretch;
				searchRow.AddChild(textToAddWidget);
				textToAddWidget.ActualTextEditWidget.EnterPressed += (object sender, KeyEventArgs keyEvent) =>
				{
					UiThread.RunOnIdle(() =>
					{
						textToAddWidget.Unfocus();
						string search = "http://www.google.com/search?q={0} silhouette&tbm=isch".FormatWith(textToAddWidget.Text);
						ApplicationController.Instance.LaunchBrowser(search);
					});
				};
			}

			// add in the invert checkbox and change image button
			var changeButton = new TextButton("Change".Localize(), theme)
			{
				BackgroundColor = theme.MinimalShade
			};
			changeButton.Click += (sender, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					// we do this using to make sure that the stream is closed before we try and insert the Picture
					AggContext.FileDialogs.OpenFileDialog(
						new OpenFileDialogParams(
							"Select an image file|*.jpg;*.png;*.bmp;*.gif;*.pdf",
							multiSelect: false,
							title: "Add Image".Localize()),
							(openParams) =>
							{
								if (!File.Exists(openParams.FileName))
								{
									return;
								}

								imageObject.AssetPath = openParams.FileName;
								imageObject.Mesh = null;

								thumbnailWidget.Image = SetImage(theme, imageObject);

								column.Invalidate();
								imageObject.Invalidate(new InvalidateArgs(imageObject, InvalidateType.Image));
							});
				});
			};

			var row = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit,
			};
			imageSection.ContentPanel.AddChild(row);

			// Invert checkbox
			var invertCheckbox = new CheckBox(new CheckBoxViewText("Invert".Localize(), textColor: theme.Colors.PrimaryTextColor))
			{
				Checked = imageObject.Invert,
				Margin = new BorderDouble(0),
			};
			invertCheckbox.CheckedStateChanged += (s, e) =>
			{
				imageObject.Invert = invertCheckbox.Checked;
			};
			row.AddChild(invertCheckbox);

			row.AddChild(new HorizontalSpacer());

			row.AddChild(changeButton);

			imageObject.Invalidated += (s, e) =>
			{
				if (e.InvalidateType == InvalidateType.Image
					&& activeImage != imageObject.Image)
				{
					thumbnailImage = SetImage(theme, imageObject);
					thumbnailWidget.Image = thumbnailImage;

					activeImage = imageObject.Image;
				}
			};

			return column;
		}

		private ImageBuffer SetImage(ThemeConfig theme, ImageObject3D imageObject)
		{
			var image = imageObject.Image;
			// Show image load error if needed
			if (image == null)
			{
				image = new ImageBuffer(185, 185).SetPreMultiply();
				var graphics2D = image.NewGraphics2D();

				graphics2D.FillRectangle(0, 0, 185, 185, theme.MinimalShade);
				graphics2D.Rectangle(0, 0, 185, 185, theme.SlightShade);
				graphics2D.DrawString("Error Loading Image".Localize() + "...", 10, 185 / 2, baseline: Agg.Font.Baseline.BoundsCenter, color: Color.Red, pointSize: theme.DefaultFontSize, drawFromHintedCach: true);
			}

			return (image.Height <= 185) ? image : ScaleThumbnailImage(185, image);
		}

		private ImageBuffer ScaleThumbnailImage(int height, ImageBuffer imageBuffer)
		{
			if (imageBuffer.Height != height)
			{
				var factor = (double) height / imageBuffer.Height;

				int width = (int)(imageBuffer.Width * factor);

				var scaledImageBuffer = new ImageBuffer(width, height);
				scaledImageBuffer.NewGraphics2D().Render(imageBuffer, 0, 0, width, height);
				return scaledImageBuffer;
			}

			return imageBuffer;
		}

		private ImageBuffer ScaleThumbnailImage(int width, int height, ImageBuffer imageBuffer)
		{
			if (imageBuffer.Width != width)
			{
				var scaledImageBuffer = new ImageBuffer(width, height);
				scaledImageBuffer.NewGraphics2D().Render(imageBuffer, 0, 0, scaledImageBuffer.Width, scaledImageBuffer.Height);
				imageBuffer = scaledImageBuffer;
			}

			return imageBuffer;
		}

	}
}
