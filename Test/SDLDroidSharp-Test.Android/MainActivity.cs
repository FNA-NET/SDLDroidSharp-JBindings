using Android.App;
using Android.Widget;
using Android.OS;
using Org.Libsdl.App;
using Android.Views;
using Android.Util;
using SDL = SDL3.SDL;
using System.Runtime.InteropServices;

namespace SDLDroidSharp_Test.Android
{
    delegate void DglClearColor(
        float red,
        float green,
        float blue,
        float alpha
    );
    delegate void DglClear(int mask);

    [Activity(
        Label = "SDL3Droid CS",
        MainLauncher = true,
        HardwareAccelerated = true,
        ScreenOrientation = global::Android.Content.PM.ScreenOrientation.Landscape
    )]
    public class MainActivity : SDLActivity
    {
        protected override string[] GetLibraries()
        {
            return ["SDL3"];
        }

        protected override void Main()
        {
            // Enable high DPI "Retina" support. Trust us, you'll want this.
            SDL.SDL_SetHint("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");

            // Keep mouse and touch input separate.
            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_TOUCH_EVENTS, "0");
            SDL.SDL_SetHint(SDL.SDL_HINT_TOUCH_MOUSE_EVENTS, "0");
            SDL.SDL_SetHint(SDL.SDL_HINT_PEN_TOUCH_EVENTS, "0");

            SDL.SDL_RunApp(0, IntPtr.Zero, FakeMain, IntPtr.Zero);
        }

        static int FakeMain(int argc, IntPtr argv)
        {
            RealMain();
            return 0;
        }

        static void RealMain()
        {
			// This _should_ be the first real SDL call we make...
			if (!SDL.SDL_Init(
				SDL.SDL_InitFlags.SDL_INIT_VIDEO |
				SDL.SDL_InitFlags.SDL_INIT_GAMEPAD
			))
			{
				throw new Exception("SDL_Init failed: " + SDL.SDL_GetError());
			}

            string videoDriver = SDL.SDL_GetCurrentVideoDriver();
            System.Diagnostics.Debug.WriteLine("SDL_GetCurrentVideoDriver: {0}{1}", videoDriver, "");

            // OPTIONAL: Get WM. Required to set the backbuffer size to the screen size
            var displayWidth = MSingleton.Resources.DisplayMetrics.WidthPixels;
            var displayHeight = MSingleton.Resources.DisplayMetrics.HeightPixels;

            IntPtr window = SDL.SDL_CreateWindow(
                "Hello ANDROID!",
                displayWidth,
                displayHeight,
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS
            );

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);

            IntPtr glContext = SDL.SDL_GL_CreateContext(window);
            SDL.SDL_GL_MakeCurrent(window, glContext);

            SDL.SDL_DisableScreenSaver();

            DglClearColor glClearColor = (DglClearColor) Marshal.GetDelegateForFunctionPointer(
                SDL.SDL_GL_GetProcAddress("glClearColor"),
                typeof(DglClearColor)
            );
            DglClear glClear = (DglClear) Marshal.GetDelegateForFunctionPointer(
                SDL.SDL_GL_GetProcAddress("glClear"),
                typeof(DglClear)
            );

            DateTime start = DateTime.UtcNow;

            SDL.SDL_Event evt;
            DateTime now;
            TimeSpan span;
            while (true) {
                while (SDL.SDL_PollEvent(out evt) == true) {
                    if (evt.type == (uint)SDL.SDL_EventType.SDL_EVENT_QUIT) {
                        goto QUIT;
                    }
                }

                now = DateTime.UtcNow;
                span = now - start;

                float t = (float) (Math.Sin(span.TotalSeconds) * 0.5 + 0.5);

                glClearColor(t, t, t, 1f);
                glClear(0x4000); // GL_COLOR_BUFFER_BIT

                SDL.SDL_GL_SwapWindow(window);
            }

            QUIT:
            SDL.SDL_Quit();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
                SetImmersive();
        }

		private void SetImmersive()
		{
			if (System.OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				Window.SetDecorFitsSystemWindows(false);
				Window.InsetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
				//NO Color Type error.
				//Window.SetNavigationBarColor(Color.Transparent);
				Window.InsetsController.Hide(WindowInsets.Type.SystemBars());
			}
			else
			{
	#pragma warning disable CS0618
				this.Window.DecorView.SystemUiVisibility =
					(StatusBarVisibility) (SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation |
										SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation |
										SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky);
	#pragma warning restore CS0618
			}
		}
    }
}