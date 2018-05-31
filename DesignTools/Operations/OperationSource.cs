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
using System.Linq;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;

namespace MatterHackers.MatterControl.DesignTools.Operations
{
	public class OperationSource : Object3D
	{
		public OperationSource()
		{
			Name = "Source".Localize();
			Visible = false;
		}

		/// <summary>
		/// This function will return the source container and if it does not find one will:
		/// <para>find the first child of the parent widget</para>
		/// <para>remove it from paret</item>
		/// <para>create a new OperationSource</para>
		/// <para>add the first child to the OperationSource</para>
		/// <para>add the OperationSource to the parent</para>
		/// </summary>
		/// <param name="parent"></param>
		/// <returns>The existing or created OperationSource</returns>
		public static IObject3D GetOrCreateSourceContainer(IObject3D parent)
		{
			var sourceContainer = parent.Children.FirstOrDefault(c => c is OperationSource);
			if (sourceContainer == null)
			{
				sourceContainer = new OperationSource();

				// Move first child to sourceContainer
				var firstChild = parent.Children.First();
				parent.Children.Remove(firstChild);
				sourceContainer.Children.Add(firstChild);
			}

			return sourceContainer;
		}
	}
}