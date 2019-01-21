using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Android.Provider;
using Android.Graphics;
using System.IO;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.ActivityExtensions;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.IntentExtensions;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.ContextExtensions;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.AlertDialogExtensions;
using DBLogic.SCI.SafeStore.MobileApp;
using System.Threading.Tasks;
using System.Linq;
using Android.Runtime;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.AndroidUriExtensions;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.BitmapExtensions;
using DBLogic.SCI.SafeStore.MobileApp.Code.Extensions.ImageViewExtensions;

namespace DBLogic.SCI.SafeStore.MobileApp.Activities
{
    [Activity(Label = "SCI Asbestos - Survey Sheet", ConfigurationChanges = Android.Content.PM.ConfigChanges.ScreenSize | Android.Content.PM.ConfigChanges.Orientation, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class SurveySheet : Code.SecureActivity
    {

        #region Enums

        public enum Mode
        {
            Create,
            Edit,
            View
        }

        public enum ActivityRequestIDs
        {
            EditSketch
        }

        #endregion

        #region Properties

        public override int LayoutID
        {
            get
            {
                return Resource.Layout.SurveySheet;
            }
        }

        private Fragments.SurveySheetRoomListFragment ctlSurveySheetRoom
        {
            get
            {
                if (Created)
                {
                    return FragmentManager.FindFragmentById<Fragments.SurveySheetRoomListFragment>(Resource.Id.ctlSurveySheetRoom);
                }
                return null;
            }
        }

        private bool IsDirty;

        private int mintProgressMax;
        private int mintProgressCurrent;

        private bool Locked;
        private Mode CurrentMode;
        private bool NavigatingToChild;

        public WcfService.SerialisablePhotoThumbnail Sketch;
        private BusinessLogic.SurveySheet mobjForm;
        public Guid mguidFormUID;
        private bool JobCodeFound;
        #endregion

        #region Controls

        private Button btnSave
        {
            get
            {
                return GetViewByID<Button>(Resource.Id.btnSave);
            }
        }

        private Button btnCancel
        {
            get
            {
                return GetViewByID<Button>(Resource.Id.btnCancel);
            }
        }

        private Button btnUpload
        {
            get
            {
                return GetViewByID<Button>(Resource.Id.btnUpload);
            }
        }

        private Button btnDelete
        {
            get
            {
                return GetViewByID<Button>(Resource.Id.btnDelete);
            }
        }

        private Switch ModeSwitch
        {
            get
            {
                return GetViewByID<Switch>(Resource.Id.ModeSwitch);
            }
        }

        private EditText txtSheetName
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtSheetName);
            }
        }

        private LinearLayout vwProgressBar
        {
            get
            {
                return GetViewByID<LinearLayout>(Resource.Id.vwProgressBar);
            }
        }

        private ProgressBar prgSaveProgress
        {
            get
            {
                return GetViewByID<ProgressBar>(Resource.Id.prgSaveProgress);
            }
        }

        private TextView lblProgressStatus
        {
            get
            {
                return GetViewByID<TextView>(Resource.Id.lblProgressStatus);
            }
        }

        private TextView lblProgressNumber
        {
            get
            {
                return GetViewByID<TextView>(Resource.Id.lblProgressNumber);
            }
        }

        private EditText txtJobNumber
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtJob);
            }
        }

        private EditText txtDate
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtDate);
            }
        }

        private EditText txtAddress
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtAddress);
            }
        }

        private EditText txtClient
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtClientName);
            }
        }

        private EditText txtSurveyor
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtPrintName);
            }
        }

        private EditText txtReasonForSurvey
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtReasonForSurvey);
            }
        }

        private EditText txtTotalSamplesTaken
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtTotalSamplesTaken);
            }
        }


        private EditText txtWater
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtWater);
            }
        }

        private EditText txtPower
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtPower);
            }
        }

        private EditText txtParking
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtParking);
            }
        }

        private EditText txtGeneralComments
        {
            get
            {
                return GetViewByID<EditText>(Resource.Id.txtGeneralComments);
            }
        }

        private ImageView imgSketch
        {
            get
            {
                return GetViewByID<ImageView>(Resource.Id.imgSketch);
            }
        }

        private ImageButton btnEditSketch
        {
            get
            {
                return GetViewByID<ImageButton>(Resource.Id.btnSketch);
            }
        }

        private ImageButton btnNew
        {
            get
            {
                return GetViewByID<ImageButton>(Resource.Id.btnNew);
            }
        }

        private ImageButton btnRefresh
        {
            get
            {
                return GetViewByID<ImageButton>(Resource.Id.btnRefresh);
            }
        }
        #endregion

        #region Methods

        private void LoadAndPopulate()
        {
            CurrentMode = (Mode)Intent.GetIntExtra("Mode", (int)Mode.Create);
            string uid = Intent.GetStringExtra("FormUID");
            mguidFormUID = Guid.Parse(uid);

            if (Sketch == null)
            {
                Sketch = new WcfService.SerialisablePhotoThumbnail() { };
            }

            try
            {
                ctlSurveySheetRoom.mFormUID = mguidFormUID;
                ctlSurveySheetRoom.LoadData(LoggedInUser, mguidFormUID);
                ctlSurveySheetRoom.OnDataLoaded();
            }
            catch (Exception ex)
            {
                new AlertDialog.Builder(this)
                .SetIcon(Resource.Drawable.Icon)
                .SetTitle("Item list could not be loaded.")
                .SetMessage("An unexpected error occurred and the list of items on the form could not be loaded")
                .SetNeutralButton("OK.", (sender, args) => { })
                .Show();
            }

            btnUpload.Enabled = false;
            Android.Net.ConnectivityManager mngrCheckConnectivity = (Android.Net.ConnectivityManager)GetSystemService(Context.ConnectivityService);
            Android.Net.NetworkInfo CheckActiveConnection = mngrCheckConnectivity.ActiveNetworkInfo;
            if (CheckActiveConnection != null)
            {
                btnUpload.Enabled = true;
            }

            //load and populate here for edit and view modes
            if (CurrentMode != Mode.Create)
            {
                ModeSwitch.Visibility = ViewStates.Visible;
                btnDelete.Visibility = ViewStates.Visible;

                mobjForm = new BusinessLogic.SurveySheet();
                mobjForm.Load(mguidFormUID);

                txtSheetName.Text = mobjForm.SheetName;
                txtDate.Text = mobjForm.Date != DateTime.MinValue ? mobjForm.Date.ToShortDateString() : string.Empty;
                txtJobNumber.Text = mobjForm.JobNumber;
                txtAddress.Text = mobjForm.Address;
                txtClient.Text = mobjForm.Client;
                txtSurveyor.Text = mobjForm.Surveyor;
                txtTotalSamplesTaken.Text = mobjForm.TotalSamplesTaken.ToString();
                txtReasonForSurvey.Text = mobjForm.ReasonForSurvey;
                txtWater.Text = mobjForm.Water;
                txtPower.Text = mobjForm.Power;
                txtParking.Text = mobjForm.Parking;
                txtGeneralComments.Text = mobjForm.GeneralComments;

                if (mobjForm.SketchPath != null || !string.IsNullOrWhiteSpace(mobjForm.SketchPath))
                {
                    Sketch.UriPath = mobjForm.SketchPath;
                    var uriImage = Code.URI.FromPath(Sketch.UriPath);
                    imgSketch.SetImageAutoScale(uriImage.GetImage(this));
                }

                switchMode(CurrentMode);
            }
            else
            {
                txtSheetName.Text = "Survey Sheet - " + LoggedInUser.Name;
                txtDate.Text = DateTime.Now.ToShortDateString();
            }
        }

        private void switchMode(Mode switchTo)
        {
            switch (switchTo)
            {
                case Mode.Edit:
                    txtSheetName.Enabled = true;
                    txtJobNumber.Enabled = true;
                    txtDate.Enabled = true;
                    txtAddress.Enabled = true;
                    txtClient.Enabled = true;
                    txtSurveyor.Enabled = true;
                    txtReasonForSurvey.Enabled = true;
                    txtTotalSamplesTaken.Enabled = true;
                    txtWater.Enabled = true;
                    txtPower.Enabled = true;
                    txtParking.Enabled = true;
                    txtGeneralComments.Enabled = true;
                    btnEditSketch.Enabled = true;
                    btnSave.Enabled = true;
                    btnDelete.Enabled = true;
                    btnNew.Enabled = true;
                    btnRefresh.Enabled = true;
                    break;
                case Mode.View:
                    txtSheetName.Enabled = false;
                    txtJobNumber.Enabled = false;
                    txtDate.Enabled = false;
                    txtAddress.Enabled = false;
                    txtClient.Enabled = false;
                    txtSurveyor.Enabled = false;
                    txtReasonForSurvey.Enabled = false;
                    txtTotalSamplesTaken.Enabled = false;
                    txtWater.Enabled = false;
                    txtPower.Enabled = false;
                    txtParking.Enabled = false;
                    txtGeneralComments.Enabled = false;
                    btnEditSketch.Enabled = false;
                    btnSave.Enabled = false;
                    btnDelete.Enabled = false;
                    btnNew.Enabled = false;
                    btnRefresh.Enabled = false;
                    break;
            }
        }

        private void Lock()
        {
            Locked = true;

            btnSave.Enabled = false;
        }

        private void Unlock()
        {
            btnSave.Enabled = true;

            Locked = false;
        }

        private void Save(bool blnSavingWithoutMessage)
        {
            string invalidMessage = string.Empty;

            Lock();

            if (mobjForm != null)
            {
                if (CurrentMode == Mode.Create && mobjForm.SheetUID == null)
                {
                    mobjForm = new BusinessLogic.SurveySheet();
                    if (mguidFormUID != Guid.Empty)
                    {
                        mobjForm.SheetUID = mguidFormUID;
                    }
                }
            }
            else
            {
                if (CurrentMode == Mode.Create)
                {
                    mobjForm = new BusinessLogic.SurveySheet();
                    if (mguidFormUID != Guid.Empty)
                    {
                        mobjForm.SheetUID = mguidFormUID;
                    }
                }
            }

            mobjForm.SheetName = txtSheetName.Text;
            mobjForm.JobNumber = txtJobNumber.Text;

            if (!string.IsNullOrWhiteSpace(txtDate.Text))
            {
                try
                {
                    mobjForm.Date = DateTime.Parse(txtDate.Text);
                }
                catch (Exception ex)
                {
                    new AlertDialog.Builder(this)
                    .SetIcon(Resource.Drawable.Icon)
                .SetTitle("Invalid Date")
                .SetMessage("The date entered is not valid, please check the value and try again.")
                .SetNeutralButton("OK.", (sender, args) => { })
                .Show();
                }
            }

            mobjForm.Address = txtAddress.Text;
            mobjForm.Client = txtClient.Text;
            mobjForm.Surveyor = txtSurveyor.Text;

            if (!string.IsNullOrWhiteSpace(txtTotalSamplesTaken.Text))
            {
                mobjForm.TotalSamplesTaken = Convert.ToInt32(txtTotalSamplesTaken.Text);
            }

            mobjForm.ReasonForSurvey = txtReasonForSurvey.Text;
            mobjForm.Water = txtWater.Text;
            mobjForm.Power = txtPower.Text;
            mobjForm.Parking = txtParking.Text;
            mobjForm.GeneralComments = txtGeneralComments.Text;

            try
            {
                mobjForm.SketchPath = Sketch.UriPath;
            }
            catch (Exception ex)
            {
                mobjForm.SketchPath = null;
            }

            mobjForm.CreatedByUserID = LoggedInUser.ID;
            mobjForm.SaveToDevice();

            IsDirty = false;

            Unlock();

            if (!blnSavingWithoutMessage)
            {
                new AlertDialog.Builder(this)
                            .SetIcon(Resource.Drawable.Icon)
                        .SetTitle("Save Complete.")
                        .SetMessage("Survey Sheet saved successfully.")
                        .SetNeutralButton("OK.", (sender, args) => { })
                        .Show();
            }
        }

        private void StartSaveProgress(int intProgressMax, string strMessage)
        {
            mintProgressCurrent = 0;
            mintProgressMax = intProgressMax;

            SetProgressMessage(strMessage);
            IncrementSaveProgress(0);

            RunOnUiThread(() =>
            {
                vwProgressBar.Animate().SetDuration(200).Alpha(1);
            });
        }

        private void EndSaveProgress()
        {
            RunOnUiThread(() =>
            {
                SetProgressMessage("All done!");
                vwProgressBar.Animate().SetDuration(500).Alpha(0);
            });
        }

        private void IncrementSaveProgress(int intIncrease)
        {
            mintProgressCurrent += intIncrease;

            int intProgress = 100 * mintProgressCurrent / mintProgressMax;

            RunOnUiThread(() =>
            {
                prgSaveProgress.Progress = intProgress;
                lblProgressNumber.Text = $"{mintProgressCurrent}/{mintProgressMax}";
            });
        }

        private void SetProgressMessage(string strMessage)
        {
            RunOnUiThread(() =>
            {
                lblProgressStatus.Text = strMessage;
            });
        }

        private void OpenEditSketchForm()
        {
            try
            {
                var objIntent = Code.IntentHelper.NewActivity<SketchPad>(this);
                objIntent.Store(IntentExtensions.Extras.DocType, "Survey");
                StartActivityForResult(objIntent, (int)ActivityRequestIDs.EditSketch);
            }
            catch (Exception ex)
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Failed to open")
                    .SetMessage("An error occurred and the sketch pad could not be opened.")
                    .SetIcon(Resource.Drawable.Icon)
                    .SetPositiveButton("Retry", (sender, args) => { OpenEditSketchForm(); })
                    .SetNegativeButton("Exit", (sender, args) => { })
                    .SetNeutralDisplayExceptionButton(ex)
                    .Show();
            }
        }

        private void OnEditSketchActivityResult([GeneratedEnum] Result resultCode, Intent data)
        {
            if (resultCode == Result.Ok)
            {
                var strPath = data.GetString(IntentExtensions.Extras.SignatureUriPath);

                if (Sketch == null)
                {
                    Sketch = new WcfService.SerialisablePhotoThumbnail() { };
                }
                Sketch.UriPath = strPath;
                var uriImage = Code.URI.FromPath(strPath);

                imgSketch.SetImageAutoScale(uriImage.GetImage(this));
                IsDirty = true;
            }
        }

        private void Upload()
        {
            Boolean ExThrown = false;
            var dlgProgress = ProgressDialog.Show(this, "Uploading", "Please wait...", true);
            Save(true);
            Task.Factory.StartNew(() =>
            {
                if (mobjForm != null)
                {
                    WcfService.SurveySheetFormInput objUploadInput = new WcfService.SurveySheetFormInput();
                    objUploadInput.SheetName = mobjForm.SheetName;
                    objUploadInput.SheetUID = mobjForm.SheetUID;
                    objUploadInput.JobNumber = mobjForm.JobNumber;
                    objUploadInput.Address = mobjForm.Address;
                    objUploadInput.Client = mobjForm.Client;
                    objUploadInput.Surveyor = mobjForm.Surveyor;
                    objUploadInput.Date = mobjForm.Date;
                    objUploadInput.ReasonForSurvey = mobjForm.ReasonForSurvey;
                    objUploadInput.TotalSamplesTaken = mobjForm.TotalSamplesTaken;
                    objUploadInput.Water = mobjForm.Water;
                    objUploadInput.Power = mobjForm.Power;
                    objUploadInput.Parking = mobjForm.Parking;
                    objUploadInput.GeneralComments = mobjForm.GeneralComments;

                    var lstInputItems = new List<WcfService.SurveySheetRoom>();
                    mobjForm.LoadItemsForForm(); //ensure form has newly created items for import
                    foreach (BusinessLogic.SurveySheetRoom room in mobjForm.Rooms)
                    {
                        var currItem = new WcfService.SurveySheetRoom();
                        currItem.RoomUID = room.RoomUID;
                        currItem.Room = room.Room;
                        currItem.AreaBeingSampled = room.AreaBeingSampled;
                        currItem.TotalSize = room.TotalSize;
                        currItem.SampleTaken = room.SampleTaken;
                        currItem.SampleNumber = room.SampleNumber;
                        currItem.SpecificComments = room.SpecificComments;
                        lstInputItems.Add(currItem);
                    }

                    objUploadInput.Items = lstInputItems.ToArray();

                    var uriSketch = Code.URI.FromPath(mobjForm.SketchPath);
                    byte[] SketchBitmap;
                    using (var stream = new MemoryStream())
                    {
                        uriSketch.GetImage(this).Compress(Bitmap.CompressFormat.Png, 0, stream);
                        SketchBitmap = stream.ToArray();
                    }
                    objUploadInput.Sketch = SketchBitmap;

                    objUploadInput.UploadingUserID = LoggedInUser.ID;

                    if (!WcfHelper.Service().UploadSurveySheetForm(objUploadInput))
                    {
                        mobjForm.Uploaded = false;

                        new AlertDialog.Builder(this)
                            .SetIcon(Resource.Drawable.Icon)
                            .SetTitle("Upload failed.")
                            .SetMessage("Survey Sheet failed to upload.")
                            .Show();

                        return;
                    }
                }
            })
            .ContinueWith(t =>
            {
                // Remove the progress spinner.
                if (dlgProgress != null)
                {
                    dlgProgress.Dismiss();
                }

                if (ExThrown)
                {
                    new AlertDialog.Builder(this)
                    .SetIcon(Resource.Drawable.Icon)
                    .SetTitle("Unexpected Error")
                    .SetMessage("an unexpected error while uploading the document has occurred, if the problem persists please contact your administrator.")
                    .SetPositiveButton("Retry", (sender, args) => { Upload(); })
                    .SetNegativeButton("Cancel", (sender, args) => { })
                    .Show();
                }
                else
                {
                    //set statement to Uploaded
                    mobjForm.Uploaded = true;
                    Save(true);

                    new AlertDialog.Builder(this)
                            .SetIcon(Resource.Drawable.Icon)
                        .SetTitle("Upload Complete.")
                        .SetMessage("Survey Sheet uploaded successfully.")
                        .SetNeutralButton("OK.", (sender, args) => { })
                        .Show();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion

        #region Event Handlers
        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
            }
            catch (Exception ex)
            {
                var strException = ex.ToString();
                return;
            }

            LoadAndPopulate();
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);

            //Bind Event Handlers
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += btnCancel_Click;
            btnUpload.Click += btnUpload_Click;
            btnEditSketch.Click += BtnEditSketch_Click;
            btnDelete.Click += btnDelete_Click;

            ModeSwitch.CheckedChange += ModeSwitch_CheckedChange;

            btnRefresh.Click += delegate (object sender, EventArgs args)
            {
                ctlSurveySheetRoom.LoadData(LoggedInUser, mguidFormUID);
                ctlSurveySheetRoom.OnDataLoaded();
            };

            btnNew.Click += delegate (object sender, EventArgs args)
            {
                Save(true);

                if (mobjForm == null)
                {
                    mobjForm = new BusinessLogic.SurveySheet();
                    mguidFormUID = mobjForm.SheetUID;
                }

                NavigatingToChild = true;

                ctlSurveySheetRoom.OpenAssessment(true, 0, mguidFormUID);
            };

            //Set IsDirty
            txtSheetName.AfterTextChanged += MarkAsDirty;
            txtJobNumber.AfterTextChanged += txtJobNumber_AfterTextChanged;
            txtJobNumber.FocusChange += txtJobNumber_FocusChanged;
            txtDate.AfterTextChanged += MarkAsDirty;
            txtAddress.AfterTextChanged += MarkAsDirty;
            txtClient.AfterTextChanged += MarkAsDirty;
            txtSurveyor.AfterTextChanged += MarkAsDirty;
            txtReasonForSurvey.AfterTextChanged += MarkAsDirty;
            txtTotalSamplesTaken.AfterTextChanged += MarkAsDirty;
            txtWater.AfterTextChanged += MarkAsDirty;
            txtPower.AfterTextChanged += MarkAsDirty;
            txtParking.AfterTextChanged += MarkAsDirty;
            txtGeneralComments.AfterTextChanged += MarkAsDirty;
        }

        protected override void OnRestart() //Unless navigating to child activity, the app will take user back to log in
        {
            base.OnRestart();
            if (!NavigatingToChild && !ctlSurveySheetRoom.IsNavigatingToChildActivity())
            {
                Intent objIntent = new Intent(this, typeof(Login));
                StartActivity(objIntent);
                FinishAffinity();
            }
            NavigatingToChild = false;
            ctlSurveySheetRoom.SetNavigatingToChildActivity(false);
        }

        private void MarkAsDirty(object sender, EventArgs e)
        {
            IsDirty = true;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            switch (requestCode)
            {
                case (int)ActivityRequestIDs.EditSketch:
                    {
                        OnEditSketchActivityResult(resultCode, data);
                        break;
                    }
            }
        }

        private void BtnEditSketch_Click(object sender, EventArgs e)
        {
            Save(true);
            NavigatingToChild = true;
            OpenEditSketchForm();
        }

        private void txtJobNumber_AfterTextChanged(object sender, EventArgs e)
        {
            try
            {
                JobCodeFound = false;

                //using the local database even when connected as it is currently updated everytime a connection is available and the RiskAssessment activity is opened. 
                BusinessLogic.LocalDatabase db = new BusinessLogic.LocalDatabase();

                var objResult = from j in db.database.Table<WcfService.JobListResponse>()
                                where j.Code.ToLower() == txtJobNumber.Text.ToLower()
                                select j;

                if (objResult.Count() > 0)
                {
                    if (mobjForm == null)
                    {
                        mobjForm = new BusinessLogic.SurveySheet();
                        mguidFormUID = mobjForm.SheetUID;
                    }
                    mobjForm.JobNumber = txtJobNumber.Text;
                    txtAddress.Text = objResult.FirstOrDefault().Address;
                    mobjForm.Address = objResult.FirstOrDefault().Address;
                    txtClient.Text = objResult.FirstOrDefault().PolicyHolder;
                    mobjForm.Client = objResult.FirstOrDefault().PolicyHolder;
                    JobCodeFound = true;
                }

                IsDirty = true;
            }
            catch (Exception ex)
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Error finding Address.")
                    .SetMessage($"An unexpected error occurred when trying to find a job matching the given job code.")
                    .SetIcon(Resource.Drawable.Icon)
                    .SetNeutralButton("Ok", (sender2, args) => { })
                    .Show();
            }
        }

        private void txtJobNumber_FocusChanged(object sender, EventArgs e)
        {
            if (!txtJobNumber.HasFocus && !JobCodeFound)
            {
                new AlertDialog.Builder(this)
                .SetTitle("No existing job found.")
                .SetMessage($"Job fields could not be autopopulated using the given job code, the current local job list may not be up to date. Documents may still be uploaded if the Job code exists.")
                .SetIcon(Resource.Drawable.Icon)
                .SetNeutralButton("Ok", (sender2, args) => { })
                .Show();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Currently saving")
                    .SetMessage($"The Survey Sheet is still saving, and if you exit now any changes may be disregarded. Are you sure you want to exit?")
                    .SetIcon(Resource.Drawable.Icon)
                    .SetPositiveButton("Yes", (sender2, args) => { Finish(); })
                    .SetNegativeButton("No", (sender2, args) => { })
                    .Show();
            }
            else if (IsDirty)
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Unsaved changes")
                    .SetMessage("There are unsaved changes, are you sure you want to close the Survey Sheet?")
                    .SetIcon(Resource.Drawable.Icon)
                    .SetPositiveButton("Yes", (sender2, args) => { Finish(); })
                    .SetNegativeButton("No", (sender2, args) => { })
                    .Show();
            }
            else
            {
                Finish();
            }
        }

        public override void OnBackPressed()
        {
            btnCancel.CallOnClick();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Save(false);
        }

        private void ModeSwitch_CheckedChange(object sender, EventArgs e)
        {
            if (ModeSwitch.Checked)
            {
                switchMode(Mode.Edit);
            }
            else
            {
                switchMode(Mode.View);
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            Upload();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            new AlertDialog.Builder(this)
                    .SetTitle("Unsaved changes")
                    .SetMessage("Are you sure you want to permanently delete this document?")
                    .SetIcon(Resource.Drawable.Icon)
                    .SetPositiveButton("Yes", (sender2, args) =>
                    {
                        if (mobjForm != null)
                        {
                            mobjForm.Delete();
                            Finish();
                        }
                    })
                    .SetNegativeButton("No", (sender2, args) => { })
                    .Show();
        }
        #endregion
    }

}