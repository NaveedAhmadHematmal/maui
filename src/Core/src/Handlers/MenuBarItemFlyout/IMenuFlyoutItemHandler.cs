﻿#if __IOS__ || MACCATALYST
using PlatformView = UIKit.UIAction;
#elif MONOANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Microsoft.Maui.Handlers
{
	public interface IMenuFlyoutItemHandler : IMenuFlyoutItemBaseHandler<IMenuFlyoutItem>
	{
		new IMenuFlyoutItem VirtualView { get; }
		new PlatformView NativeView { get; }
	}
}
