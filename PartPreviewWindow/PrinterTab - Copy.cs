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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{

	public class PrinterTab : SimpleTabControl
	{
		public PrinterTab()
			: base ()
		{

		}
	}

	public interface ITab
	{
		GuiWidget Selector { get; }
		GuiWidget Content { get; }
		bool Selected { get; set; }
	}

	public class SimpleTabControl : GuiWidget
	{
		protected GuiWidget TabBar;
		protected GuiWidget Container;

		private List<ITab> tabs = new List<ITab>();
		private ITab activeTab;

		public SimpleTabControl()
		{
			this.TabBar = new FlowLayoutWidget();
			this.AddChild(this.TabBar);

			this.Container = new GuiWidget();
			this.AddChild(this.Container);
		}

		public IEnumerable<ITab> Tabs => tabs;

		public ITab ActiveTab
		{
			get => activeTab;
			set
			{
				if (activeTab != value)
				{
					foreach (var tab in tabs)
					{
						tab.Selected = (tab == value);
					}
				}

				activeTab = value;
			}
		}

		public void AddTab(ITab tab)
		{
			tabs.Add(tab);

			this.TabBar.AddChild(tab.Selector);
			this.Container.AddChild(tab.Content);
		}
	}





	public class PrinterTab2 : Tab
	{
		private class TabPill : FlowLayoutWidget
		{
			private TextWidget label;

			public TabPill(string tabTitle, RGBA_Bytes textColor, string imageUrl = null)
			{
				var imageWidget = new ImageWidget(new ImageBuffer(16, 16))
				{
					Margin = new BorderDouble(right: 5),
					VAnchor = VAnchor.Center
				};
				this.AddChild(imageWidget);

				label = new TextWidget(tabTitle)
				{
					TextColor = textColor,
					VAnchor = VAnchor.Center
				};
				this.AddChild(label);

				if (imageUrl != null)
				{
					ApplicationController.Instance.DownloadToImageAsync(imageWidget.Image, imageUrl, false);
				}

				this.DebugShowBounds = true;
			}

			public RGBA_Bytes TextColor
			{
				get =>  label.TextColor;
				set => label.TextColor = value;
			}

			public override string Text
			{
				get => label.Text;
				set => label.Text = value;
			}
		}

		public PrinterTab2(string tabTitle, string tabName, TabPage tabPage)
		: this(
			new TabPill(tabTitle, new RGBA_Bytes(ActiveTheme.Instance.PrimaryTextColor, 140), "https://www.google.com/s2/favicons?domain=www.printrbot.com"),
			new TabPill(tabTitle, ActiveTheme.Instance.PrimaryTextColor, "https://www.google.com/s2/favicons?domain=www.printrbot.com"),
			new TabPill(tabTitle, ActiveTheme.Instance.PrimaryTextColor, "https://www.google.com/s2/favicons?domain=www.printrbot.com"),
			tabName,
			tabPage)
		{
		}

		public PrinterTab(GuiWidget normalWidget, GuiWidget hoverWidget, GuiWidget pressedWidget, string tabName, TabPage tabPage)
			: base(tabName, normalWidget, hoverWidget, pressedWidget, tabPage)
		{
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Fit | VAnchor.Bottom;
		}

		public int BorderWidth { get; set; } = 1;
		public int borderRadius { get; set; } = 4;
		
		private RGBA_Bytes activeTabColor =  ApplicationController.Instance.Theme.PrimaryTabFillColor;
		private RGBA_Bytes inactiveTabColor = ApplicationController.Instance.Theme.SlightShade;

		public override void OnDraw(Graphics2D graphics2D)
		{
			RectangleDouble borderRectangle = LocalBounds;
			borderRectangle.ExpandToInclude(new Vector2(0, -15));

			if (BorderWidth > 0)
			{
				var r = new RoundedRect(borderRectangle, this.borderRadius);
				r.normalize_radius();

				graphics2D.Render(
					r,
					selectedWidget.Visible ? activeTabColor : inactiveTabColor);
			}

			base.OnDraw(graphics2D);
		}
	}
}
