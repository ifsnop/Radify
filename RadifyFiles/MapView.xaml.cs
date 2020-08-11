﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GMap.NET;
using System.Reflection;
using GMap.NET.MapProviders;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Drawing.Image;
using System.Windows.Forms;
using GMap.NET.WindowsPresentation;
using MessageBox = System.Windows.MessageBox;
using PGTA_WPF;

namespace PGTAWPF
{
    /// <summary>
    /// Lógica de interacción para MapView.xaml
    /// </summary>
    public partial class MapView : Page
    {

        const double LatLEBL = 41.2956183;
        const double LonLEBL = 2.095114166666667;
        const double LatBCN = 41.364371;
        const double LonBCN = 2.155633;
        const double LatCAT = 41.953386;
        const double LonCAT = 1.78876;

        public List<CATALL> List = new List<CATALL>();
        public int time;
        public int starttime;
        int First_time;
        public int Min_First_time = 0;
        public int Last_time;
        public bool started = false;

        bool searchflightfromtable = false;// = false;
                                           //  bool nolongeravailable = false;
        CATALL SearchingFlight;
        bool FollowPlane = false;
        int markertype; //0=Old; 1=new
        DataTable InfoMarker = new DataTable();
        CustomActualGmapMarker mark;
        DispatcherTimer timer = new DispatcherTimer();

        Airport airport;
        MainWindow Form;
        public List<CustomActualGmapMarker> ActualMarkers = new List<CustomActualGmapMarker>();
        public List<CustomOldGmapMarker> OldMarkers = new List<CustomOldGmapMarker>();
        public List<CustomActualGmapMarker> ActualNotRefreshedMarkers = new List<CustomActualGmapMarker>();

        List<CustomOldGmapMarker> ListFlight = new List<CustomOldGmapMarker>();

        bool Helpvisible;
        int firstday;
        int lastday;
        int days;

        /// <summary>
        /// HeyHeyHey
        /// </summary>
        public MapView()
        {
            InitializeComponent();
            NoMarkerSelected(true);
            selectspeed(X1);
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += timer_Tick;
            CloseHelp();
        }

        private void MapForm_load(object sender, RoutedEventArgs e)
        {
            AlertVisible(false);
            ShowFinalhour();
            ShowStarthour();
            CheckBoxshowsmr.IsChecked = true;
            CheckBoxshowmlat.IsChecked = true;
            CheckBoxshowadsb.IsChecked = true;
            ShowMarkerInfoPanel(false);
            MarkerInfoViewLoad();
            if (searchflightfromtable == true)
            {
                CATALL message = SearchingFlight;
                mark = NewMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                NoMarkerSelected(false);
                time = message.Time_Of_day;
                ComputeTime(time);
                starttime = time;
                ShowStarthour();
                markertype = 1;
                ShowMarkerinfoOntable(mark);
                GetListFlight(mark);
                if (mark.TargetAddress != null || mark.Callsign != null || mark.Track_number != null)
                {
                    ShowFlightHistory.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowFlightHistory.IsChecked = false;
                    ShowFlightHistory.Visibility = Visibility.Hidden;
                }
                gMapControl1.Position = mark.p;
                gMapControl1.Zoom = 15;
            }
            ShowMarkersOnMap();
            Mouse.OverrideCursor = null;
        }

        private void selectspeed(TextBlock tex)
        {
            SolidColorBrush grey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
            X1.Background = grey;
            X2.Background = grey;
            X5.Background = grey;
            X10.Background = grey;
            X20.Background = grey;
            tex.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));
        }

        private void mapView_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            GMapProvider.WebProxy = WebRequest.GetSystemWebProxy();
            GMapProvider.WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButton.Left;
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.MinZoom = 2;
            gMapControl1.MaxZoom = 20;
            gMapControl1.Zoom = zoom;
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
            gMapControl1.ShowCenter = false;
        }

        public void NoMarkerSelected(bool a)
        {
            if (a == true)
            {
                NoMarkerSelectedRow.Height = new GridLength(1, GridUnitType.Auto);
                MarkerSelectedRow1.Height = new GridLength(0);
                MarkerSelectedRow2.Height = new GridLength(0);
                MarkerSelectedRow3.Height = new GridLength(0);
            }
            else
            {
                NoMarkerSelectedRow.Height = new GridLength(0);
                MarkerSelectedRow1.Height = new GridLength(1, GridUnitType.Auto);
                MarkerSelectedRow2.Height = new GridLength(1, GridUnitType.Auto);
                MarkerSelectedRow3.Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        int zoom;
        public void GetList(List<CATALL> list, List<int> AirportCodes)
        {
            if (list.Count() == 0) { time = 0; }
            else
            {
                ComputeTimeOfDay(list);
                if (AirportCodes.Count > 1)
                {
                    ViewAirport.Visibility = Visibility.Hidden;
                    ViewCity.Visibility = Visibility.Hidden;
                    ViewCCAAName.Text = "Spain";
                    if (searchflightfromtable == false)
                    {
                        gMapControl1.Position = new PointLatLng(40.434108, -3.275611);
                        zoom = 6;
                    }
                }
                else
                {
                    AirportsList AirpList = new AirportsList();
                    ViewAirport.Visibility = Visibility.Visible;
                    ViewCity.Visibility = Visibility.Visible;
                    this.airport = AirpList.Airportslist[AirportCodes[0]];
                    ViewCityName.Text = airport.CityName;
                    ViewAirportName.Text = airport.AirportName;
                    ViewCCAAName.Text = airport.CCAAName;
                    if (searchflightfromtable == false)
                    {
                        gMapControl1.Position = airport.AirportCoords;
                        zoom = 15;
                    }
                }
                if (mark != null)
                {
                    gMapControl1.Position = mark.p;
                    if (gMapControl1.Zoom < 15) { gMapControl1.Zoom = 15; }
                    FollowPlane = true;
                    markertype = 1;
                }

            }
        }


        private void ComputeTimeOfDay(List<CATALL> list)
        {
            this.List = list.OrderBy(CATAll => CATAll.List_Time_Of_Day).ToList();
            int firsttime = List[0].List_Time_Of_Day;
            int lasttime = List[List.Count - 1].List_Time_Of_Day;
            double da = (lasttime) / 86400;
            days = Convert.ToInt32(Math.Truncate(da)) + 1;
            if (List[0].List_Time_Of_Day < 0)
            {
                double fir = (firsttime / 86400);
                firstday = Convert.ToInt32(Math.Truncate(fir)) - 1;
                foreach (CATALL mess in List)
                {
                    mess.Time_Of_day = mess.List_Time_Of_Day + (-firstday * 86400);
                }
                firstday = 0;
                lastday = days;
            }
            else
            {
                foreach (CATALL mess in List)
                {
                    mess.Time_Of_day = mess.List_Time_Of_Day;
                }
                firstday = 0;
                lastday = days;
            }

            this.time = List[0].Time_Of_day-1;
            this.starttime = List[0].Time_Of_day-1;
            First_time = List[0].Time_Of_day-1;
            Last_time =(days-1)*86400+86400;

            if (days == 1)
            {
                StartDayText.Visibility = Visibility.Hidden;
                doubledotlabel0.Visibility = Visibility.Hidden;
                FinalDayText.Visibility = Visibility.Hidden;
                doubledotlabel1.Visibility = Visibility.Hidden;
                Fromlabel.Margin = new System.Windows.Thickness(0, 0, 0, 7);
                ToLabel.Margin = new System.Windows.Thickness(0, 0, 0, 7);
                HelpLabel.Text = "You can write directly to which moment of the simulation you want to go. The program automatically chooses the time of the first message received, but feel free to change it if you want. Realize that in order to change the time you must pause the simulation first.";
            }
            else
            {
                StartDayText.Visibility = Visibility.Visible;
                doubledotlabel0.Visibility = Visibility.Visible;
                FinalDayText.Visibility = Visibility.Visible;
                doubledotlabel1.Visibility = Visibility.Visible;
                Fromlabel.Margin = new System.Windows.Thickness(0,0,25,7);
                ToLabel.Margin = new System.Windows.Thickness(0,0,25,7);
                HelpLabel.Text = "You can write directly to which moment of the simulation you want to go. The program automatically chooses the time of the first message received, but feel free to change it if you want. Realize that in order to change the time you must pause the simulation first. Since you have chosen files of several days, you have four parameters to change. The format corresponds to DD:HH:MM:SS, and not HH:MM:SS:MILLISECONDS.";
            }
        }

        public void GetForm(MainWindow Form)
        {
            this.Form = Form;
        }

        public void Pause()
        {
            timer.Stop();
            started = false;
            StartDayText.IsReadOnly = false;
            StartDayText.Cursor = Cursors.IBeam;
            StartHoursText.IsReadOnly = false;
            StartHoursText.Cursor = Cursors.IBeam;
            StartMinutesText.IsReadOnly = false;
            StartMinutesText.Cursor = System.Windows.Input.Cursors.IBeam;
            StartSecondsText.IsReadOnly = false;
            StartSecondsText.Cursor = Cursors.IBeam;
            FinalDayText.IsReadOnly = false;
            FinalDayText.Cursor = Cursors.IBeam;
            FinalHoursText.IsReadOnly = false;
            FinalHoursText.Cursor = Cursors.IBeam;
            FinalMinutesText.IsReadOnly = false;
            FinalMinutesText.Cursor = System.Windows.Input.Cursors.IBeam;
            FinalSecondsText.IsReadOnly = false;
            FinalSecondsText.Cursor = Cursors.IBeam;
            Play.Visibility = Visibility.Visible;
            PauseBut.Visibility = Visibility.Hidden;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (time >= Last_time)
            {
                started = false;
                CanChangeHour(true);
                timer.Stop();
                Play.Visibility = Visibility.Visible;
                PauseBut.Visibility = Visibility.Hidden;
            }
            else
            {
                time++;
                TimeIncreasOne();
                ShowFinalhour();
                ShowStarthour();
            }
        }


        private void TimeIncreasOne()
        {
            bool first_found = false;
            int s = 0;
            foreach (CustomActualGmapMarker marker in ActualMarkers) 
            {
                if ((time - marker.Time) >= marker.refreshratio)
                {
                    AddActualNotRefreshedMarker(marker);
                }
            }
            foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers) 
            {
                if( (time-marker.Time)>=marker.refreshratio*3) 
                {
                    if (OldMarkers.Count()>50000) { OldMarkers.RemoveAt(0); }
                    AddOldMarker(marker);
                    if (mark != null)
                    {
                        if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                        {
                            ListFlight.Add(NewOldMarkernoCallsign(marker));
                        }
                    }
                    
                }
            }
            ActualNotRefreshedMarkers.RemoveAll(x => (x.Time - time) <= - x.refreshratio*3);
            //foreach (CustomActualGmapMarker marker in ActualMarkers)  { ActualNotRefreshedMarkers.Add(marker); }
            ActualMarkers.RemoveAll(x => (x.Time - time) <= -x.refreshratio);
            try
            {
                for (int i = 0; first_found == false; i++) { if (List[i].Time_Of_day == time) { first_found = true; s = i; }; }

                while (List[s].Time_Of_day == time)
                {
                    CATALL message = List[s];
                    if (message.Latitude_in_WGS_84 != -200 && message.Longitude_in_WGS_84 != -200)
                    {
                        bool DuplicatedTarget = false;
                        bool DuplicatedTrackNumber = false;
                        if (message.Target_Address != null) { DuplicatedTarget = ActualMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                        else { DuplicatedTrackNumber = ActualMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                        if (DuplicatedTarget == false && DuplicatedTrackNumber == false)
                        {

                            if (mark != null && message.Target_Address == mark.TargetAddress && mark.TargetAddress != null && mark.DetectionMode == message.DetectionMode)
                            {
                                mark = NewMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                markertype = 1;
                            }
                            else if (mark != null && mark.Track_number != null && message.Track_number == mark.Track_number && mark.DetectionMode == message.DetectionMode)
                            {
                                mark = NewMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                markertype = 1;
                            }

                            foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                            {
                                if (((marker.TargetAddress == message.Target_Address && marker.TargetAddress != null) || (marker.Track_number == message.Track_number && marker.Track_number != null) || (marker.Callsign == message.Target_Identification && marker.Callsign != null)) && marker.DetectionMode == message.DetectionMode) 
                                {
                                    if (OldMarkers.Count() > 50000) { OldMarkers.RemoveAt(0); }
                                    AddOldMarker(marker);
                                    if (mark != null)
                                    {
                                        if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                                        {
                                            ListFlight.Add(NewOldMarkernoCallsign(marker));
                                        }
                                    }
                                }
                            }
                            foreach (CustomActualGmapMarker marker in ActualMarkers)
                            {
                                if (((marker.TargetAddress == message.Target_Address && marker.TargetAddress != null) || (marker.Track_number == message.Track_number && marker.Track_number != null) || (marker.Callsign == message.Target_Identification && marker.Callsign != null)) && marker.DetectionMode == message.DetectionMode)
                                {
                                    if (OldMarkers.Count() > 50000) { OldMarkers.RemoveAt(0); }
                                    AddOldMarker(marker);
                                    if (mark != null)
                                    {
                                        if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                                        {
                                            ListFlight.Add(NewOldMarkernoCallsign(marker));
                                        }
                                    }
                                }
                            }
                            ActualNotRefreshedMarkers.RemoveAll(item => (((item.TargetAddress == message.Target_Address && item.TargetAddress != null) || (item.Track_number == message.Track_number && item.Track_number != null) || (item.Callsign == message.Target_Identification && item.Callsign != null)) && item.DetectionMode == message.DetectionMode));
                            ActualMarkers.RemoveAll(item => (((item.TargetAddress == message.Target_Address && item.TargetAddress != null) || (item.Track_number == message.Track_number && item.Track_number != null) || (item.Callsign == message.Target_Identification && item.Callsign != null)) && item.DetectionMode == message.DetectionMode));
                            AddActualMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, time, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                        }
                    }
                    s++;

                }
            }
            catch { }
            if (mark != null)
            {
                ShowMarkerinfoOntable(mark);
                if (FollowPlane == true && markertype == 1) { gMapControl1.Position = mark.p; }
            }
            if (mark != null)
            {

            }
            //&& ShowFlightHistory.IsChecked == true) { GetListFlight(mark); }
            List<MeasureLine> NewList = new List<MeasureLine>();
            LabelsList.Clear();
            for (int i = 0; i < LinesList.Count(); i++)
            {

                CustomActualGmapMarker marker1 = GetActualMarker(LinesList[i].marker1);
                if (marker1 == null) { marker1 = LinesList[i].marker1; }
                CustomActualGmapMarker marker2;
                List<PointLatLng> ListPoints = new List<PointLatLng>();
                PointLatLng p;
                PointLatLng p2;
                p = marker1.p;
            //    MeasureLine line;
                if (LinesList[i].marker2 != null)
                {
                    if (LinesList[i].OldMarker == false)
                    {
                        marker2 = GetActualMarker(LinesList[i].marker2);
                        if (marker2 == null) { marker2 = LinesList[i].marker2; }
                        NewList.Add(CreateLineTwoMarkers(marker1, marker2, false));
                    }
                    else
                    {
                        marker2 = LinesList[i].marker2;
                        NewList.Add(CreateLineTwoMarkers(marker1, marker2, true));
                    }
                }
                else
                {
                    p2 = LinesList[i].p2;
                    NewList.Add(CreateLine(p, p2, marker1));
                }

                marker1 = null;
                marker2 = null;
            }
            LinesList = NewList;
            if (OldMarkers.Count > 0) { starttime = OldMarkers.Min(CustomOldGmapMarker => CustomOldGmapMarker.Time) -1; }
            ShowMarkersOnMap();
        }

        
        private CustomActualGmapMarker GetActualMarker (CustomActualGmapMarker marker0)
        {
            CustomActualGmapMarker marker1 = null; 
            foreach (CustomActualGmapMarker marker in ActualMarkers)
            {
                if (marker0.TargetAddress != null && marker0.TargetAddress == marker.TargetAddress && marker0.DetectionMode == marker.DetectionMode)
                {
                    marker1 = marker;
                }
                else if (marker0.Callsign!= null && marker0.Callsign==marker.Callsign && marker0.DetectionMode==marker.DetectionMode)
                {
                    marker1 = marker;
                }
                else if (marker0.Track_number != null && marker0.Track_number == marker.Track_number && marker0.DetectionMode == marker.DetectionMode)
                {
                    marker1 = marker;
                }               
            }
            return marker1;
        }



        
        //FUNCTIONS TO CREATE MARKERS//
        private void AddActualMarker(double X, double Y, string Callsign, int time, int num, string emmiter, string TargetAdd, string detectionmode, string CAT, string SIC, string SAC, string Flight_level, string Track_number, int direction, int refreshratio)
        {
            PointLatLng coordinates = new PointLatLng(X, Y);
            CustomActualGmapMarker marker = new CustomActualGmapMarker(coordinates, Callsign, time, num, emmiter, TargetAdd, detectionmode, CAT, SIC, SAC, Flight_level, Track_number, direction, refreshratio);
            ActualMarkers.Add(marker);
            SetMarkerShape(marker);
        }

        private CustomActualGmapMarker NewMarker(double X, double Y, string Callsign, int time, int num, string emmiter, string TargetAdd, string detectionmode, string CAT, string SIC, string SAC, string Flight_level, string Track_number, int direction, int refreshratio)
        {
            PointLatLng coordinates = new PointLatLng(X, Y);
            CustomActualGmapMarker marker = new CustomActualGmapMarker(coordinates, Callsign, time, num, emmiter, TargetAdd, detectionmode, CAT, SIC, SAC, Flight_level, Track_number, direction, refreshratio);
            SetMarkerShape(marker);
            return marker;
        }

        private void AddNewOldMarker(double X, double Y, string Callsign, int time, int num, string emitter, string TargetAdd, string Detectionmode, string CAT, string SIC, string SAC, string Flight_level, string Track_number, int direction, int refreshratio)
        {
            PointLatLng coordinates = new PointLatLng(X, Y);
            CustomOldGmapMarker marker = new CustomOldGmapMarker(coordinates, Callsign, 0, time, num, emitter, TargetAdd, Detectionmode, CAT, SIC, SAC, Flight_level, Track_number, direction, refreshratio);
            SetMarkerShape(marker);
            OldMarkers.Add(marker);
        }

        private CustomOldGmapMarker NewOldMarkernoCallsign(CustomOldGmapMarker marker)
        {
            CustomOldGmapMarker oldmarker = new CustomOldGmapMarker(marker.p, marker.Callsign, 1, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            SetMarkerNoCallsignShape(oldmarker);
            return oldmarker;
        }

        private CustomOldGmapMarker NewOldMarkernoCallsign(CustomActualGmapMarker marker)
        {
            CustomOldGmapMarker oldmarker = new CustomOldGmapMarker(marker.p, marker.Callsign, 1, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            SetMarkerNoCallsignShape(oldmarker);
            return oldmarker;
        }

        private void AddMarkerToFlightList(CATALL message)
        {
            CustomOldGmapMarker oldmarker = new CustomOldGmapMarker(new PointLatLng(message.Latitude_in_WGS_84, message.Longitude_in_WGS_84), message.Target_Identification, 1, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
            SetMarkerNoCallsignShape(oldmarker);
            ListFlight.Add(oldmarker);
           // return oldmarker;
        }

        private CustomActualGmapMarker NewActualMarkerFromOld(CustomOldGmapMarker marker)
        {
            CustomActualGmapMarker Actualmarker = new CustomActualGmapMarker(marker.p, marker.Callsign, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            SetMarkerShape(marker);
            return Actualmarker;
        }

        private void AddOldMarker(CustomActualGmapMarker marker)
        {
            CustomOldGmapMarker oldmarker = new CustomOldGmapMarker(marker.p, marker.Callsign, 0, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            SetMarkerShape(oldmarker);
            OldMarkers.Add(oldmarker);
        }

        private void AddActualNotRefreshedMarker(CustomActualGmapMarker marker)
        {
            CustomActualGmapMarker NotRefresehd = new CustomActualGmapMarker(marker.p, marker.Callsign, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            SetRedMarkerShape(NotRefresehd);
           ActualNotRefreshedMarkers.Add(NotRefresehd);
        }

        private CustomOldGmapMarker NewOldmarkerfromactual(CustomActualGmapMarker marker)
        {
            CustomOldGmapMarker oldmarker = new CustomOldGmapMarker(marker.p, marker.Callsign, 0, marker.Time, marker.number, marker.emitter, marker.TargetAddress, marker.DetectionMode, marker.CAT, marker.SIC, marker.SAC, marker.Flight_level, marker.Track_number, marker.direction, marker.refreshratio);
            return oldmarker;
        }



        //CREATING LINES

        bool creatingline = false;
        private void AddLineClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Cross;
            creatingline = true;
        }

        List<MeasureLine> LinesList = new List<MeasureLine>();

        private MeasureLine CreateLine(PointLatLng p, PointLatLng p2, CustomActualGmapMarker marker)
        {
            List<PointLatLng> ListPoints = new List<PointLatLng>();
            ListPoints.Add(p);
            ListPoints.Add(p2);
            //int num;
            //if (LinesList.Count == 0) { num = 0; }
            //else { num = ((LinesList[LinesList.Count() - 1].num) + 1); }
            int a = num;
            MeasureLine line = new MeasureLine(p, p2, ListPoints, marker, a);
            num++;
            line.RegenerateShape(gMapControl1);
            SolidColorBrush color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)160, (byte)160, (byte)160));
            (line.Shape as System.Windows.Shapes.Path).Stroke = color;
            (line.Shape as System.Windows.Shapes.Path).StrokeThickness = 2;
            (line.Shape as System.Windows.Shapes.Path).Effect = null;
            line.Shape.MouseRightButtonUp += MouseRightButtonUpline;
            CreateLabel(line);
            return line;
        }


        private void AddLineLostFocus(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
            if (gMapControl1.IsFocused == false)
            {
                creatingline = false;
                Mouse.OverrideCursor = null;
            }
        }

        private void mapclick(object sender, MouseButtonEventArgs e)
        {
            if (creatingline == true)
            {
                System.Windows.Point mousePoint = e.GetPosition(gMapControl1);
                double lat = gMapControl1.FromLocalToLatLng(Convert.ToInt32(mousePoint.X), Convert.ToInt32(mousePoint.Y)).Lat;
                double lng = gMapControl1.FromLocalToLatLng(Convert.ToInt32(mousePoint.X), Convert.ToInt32(mousePoint.Y)).Lng;
                if (mark != null)
                {
                    LinesList.Add(CreateLine(mark.p, new PointLatLng(lat, lng), mark));
                    ShowMarkersOnMap();
                }
            }
            Mouse.OverrideCursor = null;
            creatingline = false;
        }

        private void MouseRightButtonUpline(System.Object sender, RoutedEventArgs e)
        {
            var baseobj = sender as FrameworkElement;
            var line = baseobj.DataContext as MeasureLine;
          //  List<LinesLabel> newLabelsList = new List<LinesLabel>();
            List<MeasureLine> newLineList = new List<MeasureLine>();
            int i = line.num;
            foreach (MeasureLine lin in LinesList)
            {
                if (lin.num != i) { newLineList.Add(lin); }
           //     MessageBox.Show(Convert.ToString(lin.num));
            }
            // MessageBox.Show(Convert.ToString(i) + "  " Convert.ToString())
            LinesList = newLineList;
            LabelsList.Clear();
            foreach(MeasureLine lin in LinesList) { CreateLabel(lin); }
            ShowMarkersOnMap();
           // MessageBox.Show(Convert.ToString(LabelsList.Count()) + " " + Convert.ToString(LinesList.Count())+ " "+ Convert.ToString(i));

        }


        private void LabelRightButton(System.Object sender, RoutedEventArgs e)
        {
            var baseobj = sender as FrameworkElement;
            var linelabel = baseobj.DataContext as LinesLabel;
            int i = linelabel.num;
            List<MeasureLine> newLineList = new List<MeasureLine>();
            foreach (MeasureLine line in LinesList)
            {
                if (line.num != i) { newLineList.Add(line); }
            }
            LabelsList.Remove(linelabel);
            LinesList = newLineList;

            ShowMarkersOnMap();
         //   MessageBox.Show(Convert.ToString(LabelsList.Count())+" "+ Convert.ToString(LinesList.Count()) + " " + Convert.ToString(i));


        }

        List<LinesLabel> LabelsList = new List<LinesLabel>();

        private void CreateLabel(MeasureLine line)
        {
            PointLatLng p = new PointLatLng(line.p.Lat + ((line.p2.Lat - line.p.Lat) / 2), line.p.Lng + ((line.p2.Lng - line.p.Lng) / 2));

            LinesLabel linelabel = new LinesLabel(p, line.num);
            linelabel.caption = line.ComputeParameters();
            SetMarkerShape(linelabel);
            LabelsList.Add(linelabel);
        }


        //FUNTIONS TO ADD MARKERS  AND LINES SHAPES//
        private void SetMarkerShape(CustomActualGmapMarker marker)
        {
            Bitmap bitmaptxt = MarkersDrawings.InsertText(marker);
            int heig = 50; //35
            int wid = 50; //35
            marker.Shape = new System.Windows.Controls.Image
            {

                Width = heig,
                Height = wid,
                Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
            };
            marker.Offset = new System.Windows.Point((-wid / 2), (-heig / 2) - 5);

            bitmaptxt.Dispose();
            bitmaptxt = null;
            marker.Shape.MouseLeftButtonUp += markerclick;
        }

        private void SetRedMarkerShape(CustomActualGmapMarker marker)
        {
            Bitmap bitmaptxt = MarkersDrawings.InsertTextRedImage(marker);
            int heig = 50; //35
            int wid = 50; //35
            marker.Shape = new System.Windows.Controls.Image
            {

                Width = heig,
                Height = wid,
                Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
            };
            marker.Offset = new System.Windows.Point((-wid / 2), (-heig / 2) - 5);

            bitmaptxt.Dispose();
            bitmaptxt = null;
            marker.Shape.MouseLeftButtonUp += markerclick;
        }



        private void SetMarkerShape(CustomOldGmapMarker marker)
        {
            try
            {
                Bitmap bitmaptxt = MarkersDrawings.InsertText(marker);
                int heig = 50;
                int wid = 50;
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    marker.Shape = new System.Windows.Controls.Image
                    {
                        Width = wid,
                        Height = heig,
                        Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
                    };
                });
                marker.Offset = new System.Windows.Point((-wid / 2), (-heig / 2) - 5);
                bitmaptxt.Dispose();

                bitmaptxt = null;
                //if ((OldMarkers.Count() % 1000) == 0 && OldMarkers.Count() > 500) { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true); }
            }
            catch
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                Bitmap bitmaptxt = MarkersDrawings.InsertText(marker);
                int heig = 50;
                int wid = 50;
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    marker.Shape = new System.Windows.Controls.Image
                    {
                        Width = wid,
                        Height = heig,
                        Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
                    };
                });
                marker.Offset = new System.Windows.Point((-wid / 2), (-heig / 2) - 5);
                bitmaptxt.Dispose();
                bitmaptxt = null;
            }
            marker.Shape.MouseUp += markerclick;
        }

        private void SetMarkerNoCallsignShape(CustomOldGmapMarker marker)
        {
            Bitmap bitmaptxt = MarkersDrawings.GetNoTextBitmap(marker);
            int wid = 15;
            int heig = 15;
            marker.Shape = new System.Windows.Controls.Image
            {
                Width = wid,
                Height = heig,
                Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
            };
            bitmaptxt.Dispose();
            bitmaptxt = null;
            marker.Offset = new System.Windows.Point((-wid / 2), -heig / 2);
            marker.Shape.MouseUp += markerclick;
        }





        private void ShowFinalhour()
        {
            int showingtime = time;
            int days = Convert.ToInt32(Math.Truncate(Convert.ToDouble(showingtime / 86400)));
            int hour = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - (days * 86400)) / 3600)));
            int min = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - ((days * 86400) + (hour * 3600))) / 60)));
            int sec = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - ((days * 86400) + (hour * 3600) + (min * 60))))));
            FinalHoursText.Text = Convert.ToString(hour).PadLeft(2, '0');
            FinalMinutesText.Text = Convert.ToString(min).PadLeft(2, '0');
            FinalSecondsText.Text = Convert.ToString(sec).PadLeft(2, '0');
            FinalDayText.Text = Convert.ToString(days).PadLeft(2, '0');
        }

        private void ShowStarthour()
        {
            int showingtime = starttime;
            int days = Convert.ToInt32(Math.Truncate(Convert.ToDouble(showingtime / 86400)));
            int hour = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - (days * 86400)) / 3600)));
            int min = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - ((days * 86400) + (hour * 3600))) / 60)));
            int sec = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - ((days * 86400) + (hour * 3600) + (min * 60))))));
            StartHoursText.Text = Convert.ToString(hour).PadLeft(2, '0');
            StartMinutesText.Text = Convert.ToString(min).PadLeft(2, '0');
            StartSecondsText.Text = Convert.ToString(sec).PadLeft(2, '0');
            StartDayText.Text = Convert.ToString(days).PadLeft(2, '0');
        }

        private void Play_click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TimeBoxEnter();
                started = true;
                CanChangeHour(false);
                timer.Start();
                Play.Visibility = Visibility.Hidden;
                PauseBut.Visibility = Visibility.Visible;
            }
            catch
            {
                AlertVisible(true);
                ShowFinalhour();
            }

        }

        private void CanChangeHour(bool a)
        {
            if (a==true)
            {
                                StartDayText.IsReadOnly = false;
                StartDayText.Cursor = Cursors.IBeam;
                StartHoursText.IsReadOnly = false;
                StartHoursText.Cursor = Cursors.IBeam;
                StartMinutesText.IsReadOnly = false;
                StartMinutesText.Cursor = System.Windows.Input.Cursors.IBeam;
                StartSecondsText.IsReadOnly = false;
                StartSecondsText.Cursor = Cursors.IBeam;
                FinalDayText.IsReadOnly = false;
                FinalDayText.Cursor = Cursors.IBeam;
                FinalHoursText.IsReadOnly = false;
                FinalHoursText.Cursor = Cursors.IBeam;
                FinalMinutesText.IsReadOnly = false;
                FinalMinutesText.Cursor = System.Windows.Input.Cursors.IBeam;
                FinalSecondsText.IsReadOnly = false;
            }
            else
            {
                StartDayText.Cursor = Cursors.Arrow;
                StartDayText.IsReadOnly = true;
                StartHoursText.IsReadOnly = true;
                StartHoursText.Cursor = Cursors.Arrow;
                StartMinutesText.IsReadOnly = true;
                StartMinutesText.Cursor = Cursors.Arrow;
                StartSecondsText.IsReadOnly = true;
                StartSecondsText.Cursor = Cursors.Arrow;
                FinalDayText.Cursor = Cursors.Arrow;
                FinalDayText.IsReadOnly = true;
                FinalHoursText.IsReadOnly = true;
                FinalHoursText.Cursor = Cursors.Arrow;
                FinalMinutesText.IsReadOnly = true;
                FinalMinutesText.Cursor = Cursors.Arrow;
                FinalSecondsText.IsReadOnly = true;
                FinalSecondsText.Cursor = Cursors.Arrow;
            }
        }

        private void Pause_Click(object sender, MouseButtonEventArgs e)
        {
            Pause();
        }

        private void AlertVisible(bool a)
        {
            if (a == true)
            {
                AlertIco.Visibility = Visibility.Visible;
                AlertLabel.Visibility = Visibility.Visible;
            }
            else
            {
                AlertIco.Visibility = Visibility.Hidden;
                AlertLabel.Visibility = Visibility.Hidden;
            }
        }

        private void LoadOldMarkers(int newstarttime)
        {
            int timefinal;
            if (List[List.Count - 1].Time_Of_day < time) { timefinal = List[List.Count - 1].Time_Of_day-1; }
            else { timefinal = time-1; }
            int s;
            int e=0;
            int oldstarttime = starttime;
            if (newstarttime < oldstarttime)
            {
                timefinal = oldstarttime;
                if (List[List.Count()-1].Time_Of_day<timefinal) { timefinal = List[List.Count() - 1].Time_Of_day; }
                for (s = 0; List[s].Time_Of_day < timefinal; s++) { if (List[s].Time_Of_day < newstarttime) { e = s; }; }
                if (e != 0) { e--; }
                if ((s - e) < 50000)
                {
                    for (int i = e+1; i <= s; i++)
                    {
                        CATALL message = List[i];
                        if (message.Latitude_in_WGS_84 != -200 && message.Longitude_in_WGS_84 != -200)
                        {

                            bool DuplicatedActTarget = false;
                            bool DuplicatedActTrackNumber = false;
                            if (message.Target_Address != null) { DuplicatedActTarget = ActualNotRefreshedMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                            else { DuplicatedActTrackNumber = ActualNotRefreshedMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                            if (DuplicatedActTarget == false && DuplicatedActTrackNumber == false)
                            {
                                bool DuplicatedOldTarget = false;
                                bool DuplicatedOldTrackNumber = false;
                                if (message.Target_Address != null)
                                {
                                    DuplicatedOldTarget = OldMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                                }
                                if (message.Target_Address == null)
                                {
                                    DuplicatedOldTrackNumber = OldMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                                }
                                if (DuplicatedOldTarget == false && DuplicatedOldTrackNumber == false)
                                {
                                    AddNewOldMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                }
                            }
                        }
                    }
                    starttime = newstarttime;
                }
                else
                {
                    Mouse.OverrideCursor = null;
                    OldMarkersExceed markerwarning = new OldMarkersExceed();
                    markerwarning.GetForm(this);
                    markerwarning.GetHour(this.GetHour(List[(s - 50000)].Time_Of_day));
                    markerwarning.ShowDialog();
                    if (markerwarningresult == true)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        for (int i = (s - 50000); i <= s; i++)
                        {
                            CATALL message = List[i];
                            if (message.Latitude_in_WGS_84 != -200 && message.Longitude_in_WGS_84 != -200)
                            {

                                bool DuplicatedActTarget = false;
                                bool DuplicatedActTrackNumber = false;
                                if (message.Target_Address != null) { DuplicatedActTarget = ActualNotRefreshedMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                                else { DuplicatedActTrackNumber = ActualNotRefreshedMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                                if (DuplicatedActTarget == false && DuplicatedActTrackNumber == false)
                                {
                                    bool DuplicatedOldTarget = false;
                                    bool DuplicatedOldTrackNumber = false;
                                    if (message.Target_Address != null)
                                    {
                                        DuplicatedOldTarget = OldMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                                    }
                                    if (message.Target_Address == null)
                                    {
                                        DuplicatedOldTrackNumber = OldMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                                    }
                                    if (DuplicatedOldTarget == false && DuplicatedOldTrackNumber == false)
                                    {
                                        AddNewOldMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                    }
                                }
                            }
                        }
                        starttime = List[(s - 50000)].Time_Of_day;
                    }
                    else { starttime = oldstarttime; }
                }
            }
            if (newstarttime > oldstarttime)
            {
                OldMarkers.RemoveAll(item => (item.Time < newstarttime));// { ActualMarkers.Remove(item); }
                starttime = newstarttime;
            }
        }

        bool markerwarningresult;
        public void getMarkerresult(bool a)
        {
            this.markerwarningresult = a;
        }

        private string GetHour(int t)
        {
            //int timemarker = marker.Time;
            //int showingtime;
            //if (timemarker > 86400) { showingtime = timemarker - 86400; }
            //else { showingtime = timemarker; }

            int day = Convert.ToInt32(Math.Truncate(Convert.ToDouble(t / 86400)));
            int hour = Convert.ToInt32(Math.Truncate(Convert.ToDouble((t - (day * 86400)) / 3600)));
            int min = Convert.ToInt32(Math.Truncate(Convert.ToDouble((t - ((day * 86400) + (hour * 3600))) / 60)));
            int sec = Convert.ToInt32(Math.Truncate(Convert.ToDouble((t - ((day * 86400) + (hour * 3600) + (min * 60))))));
            string h = Convert.ToString(hour).PadLeft(2, '0');
            string m = Convert.ToString(min).PadLeft(2, '0');
            string s = Convert.ToString(sec).PadLeft(2, '0');
            string d = Convert.ToString(day).PadLeft(2, '0');
            string timestr;
            if (days>1) 
            {
                timestr = d + ":" + h + ":" + m + ":" + s;
            }
            else
            {
                timestr =h + ":" + m + ":" + s;

            }
            return (timestr);
        }

        private string ShowMarkerHour(CustomActualGmapMarker marker)
        {
            int timemarker = marker.Time;
            int showingtime;
            if (timemarker > 86400) { showingtime = timemarker - 86400; }
            else { showingtime = timemarker; }
            int hour = Convert.ToInt32(Math.Truncate(Convert.ToDouble(showingtime / 3600)));
            int min = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - (hour * 3600)) / 60)));
            int sec = Convert.ToInt32(Math.Truncate(Convert.ToDouble((showingtime - ((hour * 3600) + (min * 60))))));
            return (Convert.ToString(hour).PadLeft(2, '0') + ":" + Convert.ToString(min).PadLeft(2, '0') + ":" + Convert.ToString(sec).PadLeft(2, '0'));
        }

        private void GetListFlight(CustomActualGmapMarker mark0)
        {
            if (mark0 != null)
            {
                int t=0;
                foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                {
                    if (((marker.TargetAddress == mark0.TargetAddress && mark0.TargetAddress != null) || (marker.Track_number == mark0.Track_number && marker.Track_number != null) || (marker.Callsign == mark0.Callsign && mark0.Callsign != null)) && marker.DetectionMode == mark0.DetectionMode)
                    {
                        t = marker.Time;
                    }
                }
                if (t == 0 ) 
                {
                    foreach (CustomActualGmapMarker marker in ActualMarkers)
                    {
                        if (((marker.TargetAddress == mark0.TargetAddress && mark0.TargetAddress != null) || (marker.Track_number == mark0.Track_number && marker.Track_number != null) || (marker.Callsign == mark0.Callsign && mark0.Callsign != null)) && marker.DetectionMode == mark0.DetectionMode)
                        {
                            t = marker.Time;
                        }
                    }
                    if (t == 0) { t = time; } 
                }
                ListFlight.Clear();
                bool finished = false;
                for (int i = 0; finished ==false; i++)
                {
                    if (List[i].Time_Of_day == t || i == List.Count() - 2) { finished = true; }
                    CATALL message = List[i];

                    if (((message.Target_Address == mark0.TargetAddress && mark0.TargetAddress != null) || (message.Track_number == mark0.Track_number && message.Track_number != null) || (message.Target_Identification == mark0.Callsign && mark0.Callsign != null)) && message.DetectionMode == mark0.DetectionMode)
                    {
                        bool DuplicatedTarget =false;
                        bool DuplicatedTrackNum = false;
                        if (message.Target_Address != null)
                        {
                            DuplicatedTarget = ListFlight.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                        }
                        if (message.Target_Address == null)
                        {
                            DuplicatedTrackNum = ListFlight.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode);
                        }
                        if (DuplicatedTarget == false && DuplicatedTrackNum == false)
                        {
                            AddMarkerToFlightList(message);
                        }
                    }
                }
            }
        }


        private void GetListFlight(CustomOldGmapMarker mark0)
        {
            if (mark0 != null)
            {
                int t = 0;
                foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                {
                    if (((marker.TargetAddress == mark0.TargetAddress && mark0.TargetAddress != null) || (marker.Track_number == mark0.Track_number && marker.Track_number != null) || (marker.Callsign == mark0.Callsign && mark0.Callsign != null)) && marker.DetectionMode == mark0.DetectionMode)
                    {
                        t = marker.Time;
                    }
                }
                if (t == 0)
                {
                    foreach (CustomActualGmapMarker marker in ActualMarkers)
                    {
                        if (((marker.TargetAddress == mark0.TargetAddress && mark0.TargetAddress != null) || (marker.Track_number == mark0.Track_number && marker.Track_number != null) || (marker.Callsign == mark0.Callsign && mark0.Callsign != null)) && marker.DetectionMode == mark0.DetectionMode)
                        {
                            t = marker.Time;
                        }
                    }
                    if (t == 0) { t = time; }
                }
                ListFlight.Clear();
                bool finished = false;
                for (int i = 0; finished == false; i++)
                {
                    if (List[i].Time_Of_day == t || i == List.Count() - 2) { finished = true; }
                    CATALL message = List[i];
                    if (((message.Target_Address == mark0.TargetAddress && mark0.TargetAddress != null) || (message.Track_number == mark0.Track_number && message.Track_number != null) || (message.Target_Identification == mark0.Callsign && mark0.Callsign != null)) && message.DetectionMode == mark0.DetectionMode)
                    {
                        AddMarkerToFlightList(message);
                    }
                }
            }
       
        }


        private void ShowMarkersOnMap()
        {


            try
            {
                gMapControl1.Markers.Clear();
                if (ShowFlightHistory.IsChecked == true && ShowOld.IsChecked == true)
                {
                    if (mark.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == false) { foreach (CustomOldGmapMarker marker in ListFlight) { gMapControl1.Markers.Add(marker); }; }
                    if (mark.DetectionMode == "MLAT" && CheckBoxshowmlat.IsChecked == false) { foreach (CustomOldGmapMarker marker in ListFlight) { gMapControl1.Markers.Add(marker); }; }
                    if (mark.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == false) { foreach (CustomOldGmapMarker marker in ListFlight) { gMapControl1.Markers.Add(marker); }; }
                }
                if (mark != null && markertype == 0) { gMapControl1.Markers.Add(mark); }



                if (ShowOld.IsChecked == true)
                {
                    if ((ShowFlightHistory.IsChecked == true) && ((mark.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == false) || (mark.DetectionMode == "MLAT" && CheckBoxshowmlat.IsChecked == false) || (mark.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == false)))
                    {
                        if (mark.DetectionMode == "SMR")
                        {
                            foreach (CustomOldGmapMarker marker in OldMarkers)
                            {
                                if (marker.DetectionMode == "SMR")
                                {
                                    if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                                    {
                                        gMapControl1.Markers.Add(marker);
                                    }
                                }
                                if (marker.DetectionMode == "MLAT") { if (CheckBoxshowmlat.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                                if (marker.DetectionMode == "ADSB") { if (CheckBoxshowadsb.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                            }
                        }
                        else if (mark.DetectionMode == "MLAT")
                        {
                            foreach (CustomOldGmapMarker marker in OldMarkers)
                            {
                                if (marker.DetectionMode == "SMR") { if (CheckBoxshowsmr.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                                if (marker.DetectionMode == "MLAT")
                                {
                                    if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                                    {
                                        gMapControl1.Markers.Add(marker);
                                    }
                                }
                                if (marker.DetectionMode == "ADSB") { if (CheckBoxshowadsb.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                            }
                        }
                        else
                        {
                            foreach (CustomOldGmapMarker marker in OldMarkers)
                            {
                                if (marker.DetectionMode == "SMR") { if (CheckBoxshowsmr.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                                if (marker.DetectionMode == "MLAT") { if (CheckBoxshowmlat.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                                if (marker.DetectionMode == "ADSB")
                                {
                                    if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                                    {
                                        gMapControl1.Markers.Add(marker);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (CustomOldGmapMarker marker in OldMarkers)
                        {
                            if (marker.DetectionMode == "SMR") { if (CheckBoxshowsmr.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                            if (marker.DetectionMode == "MLAT") { if (CheckBoxshowmlat.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                            if (marker.DetectionMode == "ADSB") { if (CheckBoxshowadsb.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                        }

                    }
                }

                if (mark != null && markertype == 1 && ((mark.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == false) || (mark.DetectionMode == "MLAT" && CheckBoxshowmlat.IsChecked == false) || (mark.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == false))) { gMapControl1.Markers.Add(mark); }
                else if (mark != null && markertype == 0 && ((mark.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == false) || (mark.DetectionMode == "MLAT" && CheckBoxshowmlat.IsChecked == false) || (mark.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == false)))
                {
                    foreach (CustomActualGmapMarker marker in ActualMarkers)
                    {
                        if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                        {
                            gMapControl1.Markers.Add(marker);
                        }
                    }
                    foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                    {
                        if (((marker.TargetAddress == mark.TargetAddress && marker.TargetAddress != null) || (marker.Track_number == mark.Track_number && marker.Track_number != null) || (marker.Callsign == mark.Callsign && marker.Callsign != null)) && marker.DetectionMode == mark.DetectionMode)
                        {
                            gMapControl1.Markers.Add(marker);
                        }
                    }
                    if (ShowFlightHistory.IsChecked == false || ShowOld.IsChecked == false)
                    {
                        gMapControl1.Markers.Add(mark);
                    }
                    else if (ShowFlightHistory.IsChecked == true || ShowOld.IsChecked == false)
                    {
                        gMapControl1.Markers.Add(mark);
                    }
                }


                if (mark != null && markertype == 0 && ShowOld.IsChecked == false && ShowFlightHistory.IsChecked == true)
                {
                    foreach (CustomOldGmapMarker marker in OldMarkers)
                    {
                        if (((mark.TargetAddress != null && mark.TargetAddress == marker.TargetAddress) || (mark.Callsign != null && marker.Callsign == mark.Callsign) || (mark.Track_number != null && mark.Track_number == marker.Track_number)) && marker.Time == mark.Time && marker.DetectionMode == mark.DetectionMode)
                        {
                            gMapControl1.Markers.Add(marker);
                        }
                    }
                    foreach (CustomOldGmapMarker marker in ListFlight)
                    {
                        if (((mark.TargetAddress != null && mark.TargetAddress == marker.TargetAddress) || (mark.Callsign != null && marker.Callsign == mark.Callsign) || (mark.Track_number != null && mark.Track_number == marker.Track_number)) && marker.Time == mark.Time && marker.DetectionMode == mark.DetectionMode)
                        {
                            gMapControl1.Markers.Remove(marker);
                        }
                    }
                }

                if (ShowFlightHistory.IsChecked == true)
                {
                    foreach (CustomOldGmapMarker marker in ListFlight) { gMapControl1.Markers.Add(marker);};
                }

                foreach (CustomActualGmapMarker marker in ActualMarkers)
                {
                    if (marker.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == true) { gMapControl1.Markers.Add(marker); }
                    if (marker.DetectionMode == "MLAT") { if (CheckBoxshowmlat.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                    if (marker.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == true) { gMapControl1.Markers.Add(marker); }
                }
                foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                {
                    if (marker.DetectionMode == "SMR" && CheckBoxshowsmr.IsChecked == true) { gMapControl1.Markers.Add(marker); }
                    if (marker.DetectionMode == "MLAT") { if (CheckBoxshowmlat.IsChecked == true) { gMapControl1.Markers.Add(marker); }; }
                    if (marker.DetectionMode == "ADSB" && CheckBoxshowadsb.IsChecked == true) { gMapControl1.Markers.Add(marker); }
                }

                foreach (GMapPolygon line in LinesList)
                {
                    gMapControl1.Markers.Add(line);
                }
                foreach (LinesLabel label in LabelsList)
                {
                    gMapControl1.Markers.Add(label);
                }
            }
            catch { System.Windows.MessageBox.Show(Convert.ToString(gMapControl1.Markers.Count())); }
        }

        private void SetMarkerShape(LinesLabel line)
        {
            Bitmap bitmaptxt = MarkersDrawings.InsertText(line);
            int heig = 10; //35
            int wid = 120; //35
            line.Shape = new System.Windows.Controls.Image
            {

                Width = wid,
                Height = heig,
                Source = MarkersDrawings.ToBitmapImage(bitmaptxt)
            };

            line.Offset = new System.Windows.Point(-(wid / 2), -heig / 2);
            bitmaptxt.Dispose();
            bitmaptxt = null;
            line.Shape.MouseRightButtonUp += LabelRightButton;
        }

        int num = 0;
        private MeasureLine CreateLineTwoMarkers(CustomActualGmapMarker marker, CustomActualGmapMarker marker2, bool old)
        {
            List<PointLatLng> ListPoints = new List<PointLatLng>();
            PointLatLng p = marker.p;
            PointLatLng p2 = marker2.p;
            ListPoints.Add(p);
            ListPoints.Add(p2);
            //int num;
            //if (LinesList.Count==0) { num = 0; }
            //else { num=((LinesList[LinesList.Count() - 1].num) + 1); }
            int a = num;
            MeasureLine line = new MeasureLine(p, p2, ListPoints, marker,marker2,a, old);
            num++;
            line.RegenerateShape(gMapControl1);
            SolidColorBrush color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)160, (byte)160, (byte)160));
            (line.Shape as System.Windows.Shapes.Path).Stroke = color;
            (line.Shape as System.Windows.Shapes.Path).StrokeThickness =2 ;
            (line.Shape as System.Windows.Shapes.Path).Effect = null;
            line.Shape.MouseRightButtonUp += MouseRightButtonUpline;
            CreateLabel(line);
            return line;
        }



        private void markerclick(System.Object sender, RoutedEventArgs e)
        {
            if (creatingline == true)
            {
              //  MessageBox.Show("HI0");
                var baseobj = sender as FrameworkElement;
                var mark1 = baseobj.DataContext as CustomActualGmapMarker;
                if (mark1 != null)
                {
                    if (mark1 != mark)
                    {
                        LinesList.Add(CreateLineTwoMarkers(mark, mark1, false));
                    }
                }
                var baseobj1 = sender as FrameworkElement;
                var mark2 = baseobj1.DataContext as CustomOldGmapMarker;
                if (mark2 != null)
                {
                    CustomActualGmapMarker marker = NewActualMarkerFromOld(mark2);
                    if (marker != mark)
                    {
                        LinesList.Add(CreateLineTwoMarkers(mark, marker, true));
                    }
                }
                creatingline = false;
                Mouse.OverrideCursor = null;
                ShowMarkersOnMap();
            }
            else
            {
                ListFlight = new List<CustomOldGmapMarker>();
                var baseobj = sender as FrameworkElement;
                var mark1 = baseobj.DataContext as CustomActualGmapMarker;
                if (mark1 != null)
                {
                    ShowMarkerInfoPanel(true);

                    mark = mark1;
                    markertype = 1;
                    if (mark.TargetAddress == null && mark.Track_number == null && mark.Callsign == null)
                    {
                        ShowFlightHistory.IsChecked = false;
                        ShowFlightHistory.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        ShowFlightHistory.Visibility = Visibility.Visible;
                        //if (ShowFlightHistory.IsChecked == true)
                        //{
                            GetListFlight(mark);
                        //}
                    }
                  //  System.Windows.MessageBox.Show(Convert.ToString(mark.Time) + "  " + Convert.ToString(mark.refreshratio));

                    ShowMarkerinfoOntable(mark);
                    ShowMarkersOnMap();
                }
                var baseobj1 = sender as FrameworkElement;
                var mark2 = baseobj1.DataContext as CustomOldGmapMarker;
                if (mark2 != null)
                {
                    ShowMarkerInfoPanel(true);

                    CustomOldGmapMarker marktime = mark2;
                    markertype = 0;

                    if (marktime.TargetAddress == null && marktime.Track_number == null && marktime.Callsign == null)
                    {
                        ShowFlightHistory.IsChecked = false;
                        ShowFlightHistory.Visibility = Visibility.Hidden;

                    }
                    else
                    {
                        ShowFlightHistory.Visibility = Visibility.Visible;
                        //if (ShowFlightHistory.IsChecked == true)
                        //{
                            GetListFlight(marktime);
                        //}
                    }
                    mark = NewActualMarkerFromOld(marktime);
                    //CATALL mess = List.Find(x => x.num == mark.number);
                    //System.Windows.MessageBox.Show(Convert.ToString(mess.Parameters));

                    ShowMarkerinfoOntable(mark);
                    ShowMarkersOnMap();
                }

            }
        }


        private void MarkerInfoViewLoad()
        {
            MarkerInfoView.CanUserAddRows = false;
            InfoMarker.Columns.Clear();
            InfoMarker.Columns.Add("Target\nId");
            InfoMarker.Columns.Add("Target\nAddress");
            InfoMarker.Columns.Add("Track\nNumber");
            InfoMarker.Columns.Add("CAT");
            InfoMarker.Columns.Add("SIC");
            InfoMarker.Columns.Add("SAC");
            InfoMarker.Columns.Add("Detection\nMode");
            InfoMarker.Columns.Add("Time");
            InfoMarker.Columns.Add("Flight\nLevel");
            MarkerInfoView.IsReadOnly = true;
            MarkerInfoView.CanUserResizeColumns = false;
        }

        private void ShowMarkerInfoPanel(bool a)
        {
            if (a == true)
            {
                MarkerSelectedRow1.Height = new GridLength(1, GridUnitType.Auto);
                MarkerSelectedRow2.Height = new GridLength(1, GridUnitType.Auto);
                MarkerSelectedRow3.Height = new GridLength(1, GridUnitType.Auto);
                NoMarkerSelectedRow.Height = new GridLength(0);
            }
            else
            {
                MarkerSelectedRow1.Height = new GridLength(0);
                MarkerSelectedRow2.Height = new GridLength(0);
                MarkerSelectedRow3.Height = new GridLength(0);
                NoMarkerSelectedRow.Height = new GridLength(1, GridUnitType.Auto);
            }
        }



        private void ShowMarkerinfoOntable(CustomActualGmapMarker marker)
        {
            if (mark != null)
            {
                InfoMarker.Rows.Clear();
                var row = InfoMarker.NewRow();
                if (marker.CAT != null) { row["CAT"] = marker.CAT; }
                else { row["CAT"] = "No Data"; }
                if (marker.SAC != null) { row["SAC"] = marker.SAC; }
                else { row["SAC"] = "No Data"; }
                if (marker.SIC != null) { row["SIC"] = marker.SIC; }
                else { row["SIC"] = "No Data"; }
                if (marker.Callsign != null) { row["Target\nId"] = marker.Callsign; }
                else { row["Target\nId"] = "No Data"; }
                row["Detection\nMode"] = marker.DetectionMode;
                if (marker.TargetAddress != null) { row["Target\nAddress"] = marker.TargetAddress; }
                else { row["Target\nAddress"] = "No Data"; }
                if (marker.Track_number != null) { row["Track\nNumber"] = marker.Track_number; }
                else { row["Track\nNumber"] = "No Data"; }
                row["Time"] = ShowMarkerHour(marker);
                if (marker.Flight_level != null) { row["Flight\nLevel"] = marker.Flight_level; }
                else { row["Flight\nLevel"] = "No Data"; }
                InfoMarker.Rows.Add(row);
                MarkerInfoView.ItemsSource = InfoMarker.DefaultView;
                MarkerInfoView.DataContext = InfoMarker.DefaultView;
                MarkerInfoView.UpdateLayout();
            }
        }


        private void MouseWhel(object sender, MouseWheelEventArgs e)
        {
            // System.Windows.MessageBox.Show(Convert.ToString(e.Delta/));
            if (e.Delta > 0)
            {
                gMapControl1.Zoom = gMapControl1.Zoom + 1; // SystemInformation.MouseWheelScrollLines;
            }
            else
            {
                gMapControl1.Zoom = gMapControl1.Zoom - 1; // SystemInformation.MouseWheelScrollLines;

            }
        }

        private void ShowSMR_Click(object sender, RoutedEventArgs e)
        {
            ShowMarkersOnMap();
        }

        private void ShowMLAT_Click(object sender, RoutedEventArgs e)
        {
            ShowMarkersOnMap();
        }

        private void ShowADSB_Click(object sender, RoutedEventArgs e)
        {
            ShowMarkersOnMap();
        }

        private void ShowOldClick(object sender, RoutedEventArgs e)
        {
            ShowMarkersOnMap();
        }

        private void ShowVehicleHistoryClick(object sender, RoutedEventArgs e)
        {
            //try
            //{
                //ListFlight = new List<CustomOldGmapMarker>();
                //if (mark != null)
                //{
                //    if (ShowFlightHistory.IsChecked == true)
                //    {
                //        if (mark.TargetAddress != null || mark.Callsign != null || mark.Track_number != null)
                //        {
                //            GetListFlight(mark);
                //        }
                //    }
                    ShowMarkersOnMap();
            //    }
            //}
            //catch { }
        }

        private void X1_Click(object sender, MouseButtonEventArgs e)
        {
            selectspeed(X1);
            timer.Interval = TimeSpan.FromMilliseconds(1000);
        }

        private void X2_click(object sender, MouseButtonEventArgs e)
        {
            selectspeed(X2);
            timer.Interval = TimeSpan.FromMilliseconds(500);
        }

        private void X5_Click(object sender, MouseButtonEventArgs e)
        {
            selectspeed(X5);
            timer.Interval = TimeSpan.FromMilliseconds(200);
        }

        private void X10_Click(object sender, MouseButtonEventArgs e)
        {
            selectspeed(X10);
            timer.Interval = TimeSpan.FromMilliseconds(100);
        }

        private void X20_Clicl(object sender, MouseButtonEventArgs e)
        {
            selectspeed(X20);
            timer.Interval = TimeSpan.FromMilliseconds(50);
        }

        private void ChangeView_click(object sender, RoutedEventArgs e)
        {
            if (gMapControl1.MapProvider == GMapProviders.BingHybridMap)
            {
                gMapControl1.MapProvider = GMapProviders.GoogleMap;
                Viewbuttontext.Text = "Satellite";
                ChangeviewImage.Source = new BitmapImage(new Uri(@"images/Satelite View Map.png", UriKind.Relative));
            }
            else
            {
                gMapControl1.MapProvider = GMapProviders.BingHybridMap;
                Viewbuttontext.Text = "Map";
                ChangeviewImage.Source = new BitmapImage(new Uri(@"images/MapView.png", UriKind.Relative));
            }
        }

        private void ViewCat_Click(object sender, RoutedEventArgs e)
        {
            if (airport != null)
            {
                gMapControl1.Position = airport.AirportCoords;
                gMapControl1.Zoom = 10;
            }
            else
            {
                gMapControl1.Position = new PointLatLng(40.534823, -3.455667);
                gMapControl1.Zoom = 6;
            }

        }

        private void ViewBcn_Click(object sender, RoutedEventArgs e)
        {
            gMapControl1.Position = airport.AirportCoords;
            gMapControl1.Zoom = 12;
        }

        private void ViewLEBL_Click(object sender, RoutedEventArgs e)
        {
            gMapControl1.Position = airport.AirportCoords;
            gMapControl1.Zoom = 14;
        }

        private void ShowVehicleImage_click(object sender, MouseButtonEventArgs e)
        {
            ShowPlaneOnMap();
        }

        private void ShowVehicleText_click(object sender, MouseButtonEventArgs e)
        {
            ShowPlaneOnMap();
        }

        private void ShowPlaneOnMap()
        {
            if (mark != null)
            {
                gMapControl1.Position = mark.p;
                if (gMapControl1.Zoom < 15) { gMapControl1.Zoom = 15; }
                FollowPlane = true;
                markertype = 1;
            }
        }

        private void DragMapEvent(object sender, System.Windows.DragEventArgs e)
        {

            FollowPlane = false;
        }

        private void Restart_click(object sender, MouseButtonEventArgs e)
        {
            timer.Stop();
            if (List.Count() == 0) { time = 0; }
            else
            {
                time = 86400;
                foreach (CATALL message in List) { if (message.Time_Of_day < time) { time = message.Time_Of_day; }; }
            }
            time--;
            starttime = time;
            ShowFinalhour();
            ShowStarthour();
            started = false;
            CanChangeHour(true);
            PauseBut.Visibility = Visibility.Hidden;
            Play.Visibility = Visibility.Visible;
            ActualMarkers.Clear();
            ActualNotRefreshedMarkers.Clear();
            OldMarkers.Clear();
            ListFlight.Clear();
            ShowFlightHistory.IsChecked = false;
            mark = null;
            selectspeed(X1);
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            NoMarkerSelected(true);
            LinesList.Clear();
            LabelsList.Clear();

            ShowMarkersOnMap();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        }

        private void ShowOnListImage_click(object sender, MouseButtonEventArgs e)
        {
            ShowOnList();
        }

        private void ShowOnListText_click(object sender, MouseButtonEventArgs e)
        {
            ShowOnList();
        }

        public void SearchFlightInMap(CATALL flight)
        {
            time = flight.Time_Of_day;
            this.searchflightfromtable = true;
            SearchingFlight = flight;
        }



        private void ShowOnList()
        {
            if (mark != null)
            {
                timer.Stop();
                started = false;
                CanChangeHour(true);
                PauseBut.Visibility = Visibility.Hidden;
                Play.Visibility = Visibility.Visible;
                int num = mark.number;
                CATALL message = new CATALL();
                foreach (CATALL mes in List) { if (mes.num == num) { message = mes; }; }
                if (message.CAT != "") { Form.OpenFlightInList(message); }
            }
        }

        private void DragMapEvent()
        {
            FollowPlane = false;
        }

        private void ExportKMLImageClick(object sender, MouseButtonEventArgs e)
        {
            ExportKMLfun();
        }

        private void ExportKMLtextClick(object sender, MouseButtonEventArgs e)
        {
            ExportKMLfun();
        }

        private void ExportKMLfun()
        {
            timer.Stop();
            ExportKML savekml = new ExportKML();
            savekml.GetMapForm(this);
            savekml.ShowDialog();
        }

        private void CloseHelpButton(object sender, MouseButtonEventArgs e)
        {
            CloseHelp();
        }

        private void CloseHelp()
        {
            HelpRow.Height = new GridLength(0);
            Helpvisible = false;
        }

        private void OpenHelp()
        {
            HelpRow.Height = new GridLength(1, GridUnitType.Auto);
            Helpvisible = true;
        }

        private void HelpButtonClick(object sender, MouseButtonEventArgs e)
        {
            if (Helpvisible == true)
            {
                CloseHelp();
            }
            else
            {
                OpenHelp();
            }
        }

        private void secondskey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { TimeBoxEnter(); }
        }

        private void minuteskey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { TimeBoxEnter(); }
        }

        private void hourskey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { TimeBoxEnter(); }
        }

        private void dayskey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { TimeBoxEnter(); }
        }

        private void TimeBoxEnter()
        {
            try
            {
                AlertVisible(false);
                int finaldays = Convert.ToInt32(FinalDayText.Text);
                int finalmin = Convert.ToInt32(FinalMinutesText.Text);
                int finalhours = Convert.ToInt32(FinalHoursText.Text);
                int finalsec = Convert.ToInt32(FinalSecondsText.Text);
                int startdays = Convert.ToInt32(StartDayText.Text);
                int startmin = Convert.ToInt32(StartMinutesText.Text);
                int starthours = Convert.ToInt32(StartHoursText.Text);
                int startsec = Convert.ToInt32(StartSecondsText.Text);
                if (finaldays > lastday || finaldays < 0 || finalmin > 60 || finalhours > 24 || finalsec > 60 || finalmin < 0 || finalhours <0 || finalsec < 0 || startdays > lastday || startdays < 0 || startmin > 60 || starthours > 24 || startsec > 60 || startmin < 0 || starthours < 0 || startsec < 0 ) { AlertVisible(true); ShowFinalhour(); }
                else
                {
                    int previoustime = time;
                    int previousstarttime = starttime;
                    time = finaldays * 86400 + finalhours * 3600 + finalmin * 60 + finalsec;
                    int newstarttime= startdays * 86400 + starthours * 3600 + startmin * 60 + startsec;
                    if (time != previoustime)
                    {
                        Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                        ComputeTime(time);
                        ShowFinalhour();
                        starttime = time;
                        ShowStarthour();
                        ShowMarkersOnMap();
                        Mouse.OverrideCursor = null;
                    }
                    else if (time==previoustime && newstarttime!=starttime)
                    {
                        if (newstarttime <= previoustime)
                        {
                            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                            LoadOldMarkers(newstarttime);
                            //starttime = newstarttime;
                            ShowFinalhour();
                            ShowStarthour();
                            ShowMarkersOnMap();
                            Mouse.OverrideCursor = null;
                        }
                        else { AlertVisible(true); }
                        //ComputeOld;
                    }
                }
            }
            catch
            {
                AlertVisible(true);
                ShowFinalhour();
                Mouse.OverrideCursor = null;
            }
        }

        private void ComputeTime(int time)
        {
            bool first_found = false;
            int s = 0;
            OldMarkers.Clear();
            ActualNotRefreshedMarkers.Clear();
            ActualMarkers.Clear();
            try
            {
                for (int i = 0; first_found == false; i++) { if (List[i].Time_Of_day == time) { first_found = true; s = i; }; }
                while (List[s].Time_Of_day == time)
                {
                    CATALL message = List[s];
                    if (message.Latitude_in_WGS_84 != -200 && message.Longitude_in_WGS_84 != -200)
                    {
                        bool DuplicatedTarget = false;
                        bool DuplicatedTrackNumber = false;
                        if (message.Target_Address != null) { DuplicatedTarget = ActualMarkers.Any(x => x.TargetAddress == message.Target_Address && x.TargetAddress != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                        else { DuplicatedTrackNumber = ActualMarkers.Any(x => x.Track_number == message.Track_number && x.Track_number != null && x.Time == message.Time_Of_day && message.DetectionMode == x.DetectionMode); }
                        if (DuplicatedTarget == false && DuplicatedTrackNumber == false)
                        {
                            if (mark != null && message.Target_Address == mark.TargetAddress && mark.TargetAddress != null && mark.DetectionMode == message.DetectionMode)
                            {
                                mark = NewMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                markertype = 1;
                            }
                            else if (mark != null && mark.Track_number != null && message.Track_number == mark.Track_number && mark.DetectionMode == message.DetectionMode)
                            {
                                mark = NewMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, message.Time_Of_day, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                                markertype = 1;
                            }

                            foreach (CustomActualGmapMarker marker in ActualNotRefreshedMarkers)
                            {
                                if (((marker.TargetAddress == message.Target_Address && marker.TargetAddress != null) || (marker.Track_number == message.Track_number && marker.Track_number != null) || (marker.Callsign == message.Target_Identification && marker.Callsign != null)) && marker.DetectionMode == message.DetectionMode) { AddOldMarker(marker); }
                            }
                            ActualNotRefreshedMarkers.RemoveAll(item => (((item.TargetAddress == message.Target_Address && item.TargetAddress != null) || (item.Track_number == message.Track_number && item.Track_number != null) || (item.Callsign == message.Target_Identification && item.Callsign != null)) && item.DetectionMode == message.DetectionMode));// { ActualMarkers.Remove(item); }
                            AddActualMarker(Convert.ToDouble(message.Latitude_in_WGS_84), Convert.ToDouble(message.Longitude_in_WGS_84), message.Target_Identification, time, message.num, message.type, message.Target_Address, message.DetectionMode, message.CAT, message.SIC, message.SAC, message.Flight_level, message.Track_number, message.direction, message.refreshratio);
                        }
                    }
                    s++;

                }
            }
            catch { }
            if (mark != null)
            {
                ShowMarkerinfoOntable(mark);
                if (FollowPlane == true && markertype == 1) { gMapControl1.Position = mark.p; }
            }
            if (mark != null && ShowFlightHistory.IsChecked == true) { GetListFlight(mark); }
            List<MeasureLine> NewList = new List<MeasureLine>();
            LabelsList.Clear();
            for (int i = 0; i < LinesList.Count(); i++)
            {

                CustomActualGmapMarker marker1 = GetActualMarker(LinesList[i].marker1);
                if (marker1 == null) { marker1 = LinesList[i].marker1; }
                CustomActualGmapMarker marker2;
                List<PointLatLng> ListPoints = new List<PointLatLng>();
                PointLatLng p;
                PointLatLng p2;
                p = marker1.p;
                if (LinesList[i].marker2 != null)
                {
                    if (LinesList[i].OldMarker == false)
                    {
                        marker2 = GetActualMarker(LinesList[i].marker2);
                        if (marker2 == null) { marker2 = LinesList[i].marker2; }
                        NewList.Add(CreateLineTwoMarkers(marker1, marker2, false));
                    }
                    else
                    {
                        marker2 = LinesList[i].marker2;
                        NewList.Add(CreateLineTwoMarkers(marker1, marker2, true));
                    }
                }
                else
                {
                    p2 = LinesList[i].p2;
                    NewList.Add(CreateLine(p, p2, marker1));
                }

                marker1 = null;
                marker2 = null;
            }
            LinesList = NewList;
            ShowMarkersOnMap();
        }
    }
}