﻿using System;
using System.Threading.Tasks;
using WebKit;

namespace Microsoft.Maui.Platform
{
	public static class WebViewExtensions
	{
		public static void UpdateSource(this WKWebView platformWebView, IWebView webView)
		{
			platformWebView.UpdateSource(webView, null);
		}

		public static void UpdateSource(this WKWebView platformWebView, IWebView webView, IWebViewDelegate? webViewDelegate)
		{
			if (webViewDelegate != null)
			{
				webView.Source?.Load(webViewDelegate);

				platformWebView.UpdateCanGoBackForward(webView);
			}
		}

		public static void UpdateGoBack(this WKWebView platformWebView, IWebView webView)
		{
			if (platformWebView == null)
				return;

			if (platformWebView.CanGoBack)
				platformWebView.GoBack();

			platformWebView.UpdateCanGoBackForward(webView);
		}

		public static void UpdateGoForward(this WKWebView platformWebView, IWebView webView)
		{
			if (platformWebView == null)
				return;

			if (platformWebView.CanGoForward)
				platformWebView.GoForward();

			platformWebView.UpdateCanGoBackForward(webView);
		}

		public static void UpdateReload(this WKWebView platformWebView, IWebView webView)
		{
			// TODO: Sync Cookies

			platformWebView?.Reload();
		}

		internal static void UpdateCanGoBackForward(this WKWebView platformWebView, IWebView webView)
		{
			webView.CanGoBack = platformWebView.CanGoBack;
			webView.CanGoForward = platformWebView.CanGoForward;
		}

		public static void Eval(this WKWebView platformWebView, IWebView webView, string script)
		{
			platformWebView.EvaluateJavaScriptAsync(script);
		}

		public static void EvaluateJavaScript(this WKWebView webView, EvaluateJavaScriptAsyncRequest request)
		{
			request.RunAndReport(EvaluateJavaScript(webView, request.Script));
		}

		static async Task<string> EvaluateJavaScript(WKWebView webView, string script)
		{
			var result = await webView.EvaluateJavaScriptAsync(script);
			return result?.ToString() ?? "null";
		}
	}
}