﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Maui.Handlers
{
	public partial class MenuFlyoutItemHandler : MenuFlyoutItemBaseHandler<IMenuFlyoutItem, MenuFlyoutItem>
	{
		protected override MenuFlyoutItem CreateNativeElement()
		{
			return new MenuFlyoutItem();
		}
		
		protected override void ConnectHandler(MenuFlyoutItem nativeView)
		{
			base.ConnectHandler(nativeView);
			nativeView.Click += OnClicked;
		}

		protected override void DisconnectHandler(MenuFlyoutItem nativeView)
		{
			base.DisconnectHandler(nativeView);
			nativeView.Click -= OnClicked;
		}

		void OnClicked(object sender, UI.Xaml.RoutedEventArgs e)
		{
			VirtualView.Clicked();
		}

		public static void MapSource(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			handler.NativeView.Icon =
				view.Source?.ToIconSource(handler.MauiContext!)?.CreateIconElement()
		}

		public static void MapText(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			// TODO MAUI Fix the types on interfaces
			((MenuFlyoutItem)handler.NativeView!).Text = view.Text;
		}
	}
}
