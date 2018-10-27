using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Threading.Tasks;
using Plugin.FilePicker.Abstractions;
using Android.Provider;
using System.Net;
using System.Linq;

namespace Plugin.FilePicker
{
    [Activity (ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    [Preserve (AllMembers = true)]
    public class FilePickerActivity : Activity
    {
        private Context context;

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            context = Application.Context;

            var intent = new Intent (Intent.ActionGetContent);

            intent.SetType("*/*");

            string[] allowedTypes = Intent.GetStringArrayExtra("allowedTypes")?.
                Where(o => !string.IsNullOrEmpty(o) && o.Contains("/")).ToArray();

            if (allowedTypes != null && allowedTypes.Any()) {
                intent.PutExtra(Intent.ExtraMimeTypes, allowedTypes);
            }

            intent.AddCategory (Intent.CategoryOpenable);
            try {
                StartActivityForResult (Intent.CreateChooser (intent, "Select file"), 0);
            } catch (Exception exAct) {
                System.Diagnostics.Debug.Write (exAct);
            }
        }

        protected override void OnActivityResult (int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult (requestCode, resultCode, data);

            if (resultCode == Result.Canceled) {
                // Notify user file picking was cancelled.
                OnFilePickCancelled ();
                Finish ();
            } else {
                System.Diagnostics.Debug.Write (data.Data);
                try {
                    var _uri = data.Data;

                    var filePath = IOUtil.getPath (context, _uri);

                    if (string.IsNullOrEmpty (filePath))
                        filePath = IOUtil.isMediaStore(_uri.Scheme) ? _uri.ToString() : _uri.Path;

                    var fileName = GetFileName (context, _uri);

                    OnFilePicked (new FilePickerEventArgs (null, fileName, filePath));
                } catch (Exception readEx) {
                    System.Diagnostics.Debug.Write(readEx);
                    // Notify user file picking failed.
                    FilePickCancelled?.Invoke(
                        this,
                        new FilePickerCancelledEventArgs
                        {
                            Exception = readEx
                        });
                } finally {
                    Finish ();
                }
            }
        }

        string GetFileName (Context ctx, Android.Net.Uri uri)
        {

            string [] projection = { MediaStore.MediaColumns.DisplayName };

            var cr = ctx.ContentResolver;
            var name = "";
            var metaCursor = cr.Query (uri, projection, null, null, null);

            if (metaCursor != null) {
                try {
                    if (metaCursor.MoveToFirst ()) {
                        name = metaCursor.GetString (0);
                    }
                } finally {
                    metaCursor.Close ();
                }
            }

            if (!string.IsNullOrWhiteSpace(name))
                return name;
            else
                return System.IO.Path.GetFileName(WebUtility.UrlDecode(uri.ToString()));
        }

        internal static event EventHandler<FilePickerEventArgs> FilePicked;
        internal static event EventHandler<FilePickerCancelledEventArgs> FilePickCancelled;

        private static void OnFilePickCancelled ()
        {
            FilePickCancelled?.Invoke (null, null);
        }

        private static void OnFilePicked (FilePickerEventArgs e)
        {
            FilePicked?.Invoke(null, e);
        }
    }
}