﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui.Handlers
{
	public partial class MenuBarHandler : ElementHandler<IMenuBar, MenuBar>, IMenuBarHandler
	{
		public static void MapIsEnabled(IMenuBarHandler handler, IMenuBar view) =>
			handler.NativeView.UpdateIsEnabled(view.IsEnabled);

		public static void MapTextColor(IMenuBarHandler handler, IMenuBar menuBar) =>
			handler.NativeView?.UpdateTextColor(menuBar);

		public static void MapCharacterSpacing(IMenuBarHandler handler, IMenuBar menuBar) =>
			handler.NativeView?.UpdateCharacterSpacing(menuBar);

		public static void MapFont(IMenuBarHandler handler, IMenuBar menuBar)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();
			handler.NativeView?.UpdateFont(menuBar, fontManager);
		}

		protected override MenuBar CreateNativeElement()
		{
			return new MenuBar();
		}

		public override void SetVirtualView(IElement view)
		{
			base.SetVirtualView(view);
			Clear();

			foreach (var item in ((IMenuBar)view))
			{
				Add(item);
			}
		}

		public void Add(IMenuBarItem view)
		{
			NativeView.Items.Add((MenuBarItem)view.ToPlatform(MauiContext!));
		}

		public void Remove(IMenuBarItem view)
		{
			if (view.Handler != null)
				NativeView.Items.Remove((MenuBarItem)view.ToPlatform());
		}

		public void Clear()
		{
			NativeView.Items.Clear();
		}

		public void Insert(int index, IMenuBarItem view)
		{
			NativeView.Items.Insert(index, (MenuBarItem)view.ToPlatform(MauiContext!));
		}
	}
}
