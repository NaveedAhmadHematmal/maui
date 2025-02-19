﻿#nullable enable
using Microsoft.UI.Xaml.Controls;
using WBrush = Microsoft.UI.Xaml.Media.Brush;

namespace Microsoft.Maui.Handlers
{
	public partial class DatePickerHandler : ViewHandler<IDatePicker, CalendarDatePicker>
	{
		WBrush? _defaultForeground;

		protected override CalendarDatePicker CreatePlatformView() => new CalendarDatePicker();

		protected override void ConnectHandler(CalendarDatePicker platformView)
		{
			platformView.DateChanged += DateChanged;
		}

		protected override void DisconnectHandler(CalendarDatePicker platformView)
		{
			platformView.DateChanged -= DateChanged;
		}

		void SetupDefaults(CalendarDatePicker platformView)
		{
			_defaultForeground = platformView.Foreground;

			
		}

		public static void MapFormat(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateDate(datePicker);
		}

		public static void MapDate(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateDate(datePicker);
		}

		public static void MapMinimumDate(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateMinimumDate(datePicker);
		}

		public static void MapMaximumDate(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateMaximumDate(datePicker);
		}

		public static void MapCharacterSpacing(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateCharacterSpacing(datePicker);
		}

		public static void MapFont(DatePickerHandler handler, IDatePicker datePicker)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();

			handler.PlatformView?.UpdateFont(datePicker, fontManager);
		}

		public static void MapTextColor(DatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateTextColor(datePicker, handler._defaultForeground);
		}

		private void DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
		{
			if (VirtualView == null)
				return;

			if (!args.NewDate.HasValue)
			{
				return;
			}

			// TODO ezhart 2021-06-21 For the moment, IDatePicker requires a date to be selected; once that's fixed, we can uncomment these next lines

			//if (!args.NewDate.HasValue)
			//{
			//	VirtualView.Date = null;
			//	return;
			//}

			//if (VirtualView.Date == null)
			//{
			//	VirtualView.Date = args.NewDate.Value.Date;
			//}

			VirtualView.Date = args.NewDate.Value.Date;
		}
	}
}