﻿using System;
using System.Collections.Generic;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassLibrary;
using System.IO;
using MultiCAT6.Utils;
using System.Reflection;
using System.Xml.Serialization;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using Document = SharpKml.Dom.Document;
using System.Xml;
using GMap.NET.WindowsPresentation;

namespace AsterixDecoder
{
    public partial class Map : Form
    {
        // ATTRIBUTES
        // List containing TargetData of all targets
        private List<TargetData> targetData_list;
        private int list_index = 0;

        // One Overlay for each type
        private GMapOverlay SMR_overlay = new GMapOverlay("SMR");
        private GMapOverlay MLAT_overlay = new GMapOverlay("MLAT");
        private GMapOverlay ADSB_overlay = new GMapOverlay("ADSB");

        // List containing the IDs of the targets ploted, necessary for the RemovePreviousMarker() method
        private List<string> SMR_markerTags = new List<string>();
        private List<string> MLAT_markerTags = new List<string>();
        private List<string> ADSB_markerTags = new List<string>();

        // One Overlay for each type, for the traces
        private GMapOverlay SMR_traces_overlay = new GMapOverlay("SMR_traces");
        private GMapOverlay MLAT_traces_overlay = new GMapOverlay("MLAT_traces");
        private GMapOverlay ADSB_traces_overlay = new GMapOverlay("ADSB_traces");

        // Radar WGS84 Coordinates of SMR and MLAT
        private CoordinatesWGS84 SMR_radar_WGS84Coordinates = new CoordinatesWGS84(Functions.Deg2Rad(41.29561833), Functions.Deg2Rad(2.09511417));
        private CoordinatesWGS84 MLAT_radar_WGS84Coordinates = new CoordinatesWGS84(Functions.Deg2Rad(41.29706278), Functions.Deg2Rad(2.07844722));

        // Initial Position for the Map
        private PointLatLng LEBL_position = new PointLatLng(41.29839, 2.08331);

        // Current time
        private string currentTime= "08:00:00";

        // GeoUtils class
        private GeoUtils myGeoUtils = new GeoUtils();

        // Timer started indicator
        private bool timerstarted = false;


        // CONSTRUCTOR
        public Map(List<TargetData> targetData_list)
        {
            InitializeComponent();
            this.targetData_list = targetData_list;
        }


        // METHODS
        // Load GMap
        private void Map_Load(object sender, EventArgs e)
        {
            GMap_control.Show();
            GMap_control.MarkersEnabled = true;
            GMap_control.PolygonsEnabled = true;
            GMap_control.CanDragMap = false;
            GMap_control.MapProvider = GMapProviders.GoogleMap;
            
            GMap_control.Position = LEBL_position;
            GMap_control.MinZoom = 8;
            GMap_control.MaxZoom = 22;
            GMap_control.Zoom = 13;
            GMap_control.AutoScroll = true;

            // We add the overlays to the GMap
            GMap_control.Overlays.Add(this.SMR_overlay);
            GMap_control.Overlays.Add(this.MLAT_overlay);
            GMap_control.Overlays.Add(this.ADSB_overlay);

            // We add the overlays of the traces to the GMap
            GMap_control.Overlays.Add(this.SMR_traces_overlay);
            GMap_control.Overlays.Add(this.MLAT_traces_overlay);
            GMap_control.Overlays.Add(this.ADSB_traces_overlay);
            this.SMR_traces_overlay.IsVisibile = false;
            this.MLAT_traces_overlay.IsVisibile = false;
            this.ADSB_traces_overlay.IsVisibile = false;

            // We check all the SMR, MLAT and ADSB checkboxes
            SMR_checkBox.Checked = true;
            ADSB_checkBox.Checked = true;
            MLAT_checkBox.Checked = true;

            // We set the Hour label
            Time_label.Text = this.currentTime;
        }

        // Play button
        private void Play_button_Click(object sender, EventArgs e) //START 
        {
            Stop_button.Show();
            Play_button.Hide();

            timerstarted = true;
            timer1.Start();
        }

        // Stop button
        private void Stop_button_Click(object sender, EventArgs e) //STOP
        {
            Stop_button.Hide();
            Play_button.Show();

            timerstarted = false;
            timer1.Stop();
        }

        // Export to .KML button
        private void ExportKML_button_Click(object sender, EventArgs e)
        {
            Loading_ButtonState(ExportKML_button);

            // Crear un objeto Kml
            Kml kml = new Kml();
          
            // Crear el documento KML
            var document = new Document();
            //document.Name = "Mi Mapa";
            kml.Feature = document;

            // Recorrer todos los marcadores del mapa
            foreach (var marker in SMR_overlay.Markers)
            {
                // Crear un Placemark para el marcador
                var placemark = new SharpKml.Dom.Placemark();
                placemark.Name = marker.ToolTipText;
                placemark.Geometry = new SharpKml.Dom.Point()

                {
                    Coordinate = new Vector(marker.Position.Lat, marker.Position.Lng)
                };

                // Agregar el Placemark al Document
                document.AddFeature(placemark);
            }
            foreach (var marker in ADSB_overlay.Markers)
            {
                // Crear un Placemark para el marcador
                var placemark = new SharpKml.Dom.Placemark();
                placemark.Name = marker.ToolTipText;
                placemark.Geometry = new SharpKml.Dom.Point()
                
                {
                    Coordinate = new Vector(marker.Position.Lat, marker.Position.Lng)
                };
              

                // Asignar el estilo al Placemark
                placemark.StyleUrl = new Uri("#red", UriKind.Relative);

                // Agregar el Placemark al Document
                document.AddFeature(placemark);
            }
            foreach (var marker in MLAT_overlay.Markers)
            {
                // Crear un Placemark para el marcador
                var placemark = new SharpKml.Dom.Placemark();
                placemark.Name = marker.ToolTipText;
                placemark.Geometry = new SharpKml.Dom.Point()
                {
                    Coordinate = new Vector(marker.Position.Lat, marker.Position.Lng)
                };

                // Agregar el Placemark al Document
                document.AddFeature(placemark);
            }

            // Save the .KML file
            SaveFileDialog saveFile = new SaveFileDialog() { Filter = "KML|*.kml", FileName = "myMap" };
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                using (var stream = File.Create(saveFile.FileName))
                {
                    KmlFile kml1 = KmlFile.Create(document, false);
                    kml1.Save(stream);
                }
            }
            else
            {
                MessageBox.Show("Error when saving .kml file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            FinishedLoading_ButtonState(ExportKML_button, "EXPORT TO .KML");
        }

        // timer1.Tick: Method that is executed in every timer1.Interval
        private void timer1_Tick(object sender, EventArgs e)
        {
            System.TimeSpan time = System.TimeSpan.FromSeconds(System.TimeSpan.Parse(this.currentTime).TotalSeconds + 1);
            currentTime = time.ToString(@"hh\:mm\:ss");
            Time_label.Text = this.currentTime;
            CheckTargets();
        }

        // Method that is executed when a marker from the GMap_control is clicked
        private void GMap_control_OnMarkerClick(GMap.NET.WindowsForms.GMapMarker item, MouseEventArgs e)
        {
            Set_TargetData_DGV(item);
        }

        // TIME SCALE
        private void TimeScaleForward_button_Click(object sender, EventArgs e)
        {
            if (TimeScaleIndicator_label.Text == "x1")
            {
                TimeScaleIndicator_label.Text = "x1.25";
                timer1.Interval = Convert.ToInt32((1000) / (1.25));
            }
            else if (TimeScaleIndicator_label.Text == "x0.25")
            {
                TimeScaleIndicator_label.Text = "x0.5";
                timer1.Interval = (1000) * 2;
            }
            else if (TimeScaleIndicator_label.Text == "x0.5")
            {
                TimeScaleIndicator_label.Text = "x0.75";
                timer1.Interval = Convert.ToInt32(1000 / 0.75);
            }
            else if (TimeScaleIndicator_label.Text == "x0.75")
            {
                TimeScaleIndicator_label.Text = "x1";
                timer1.Interval = 1000;
            }
            else if (TimeScaleIndicator_label.Text == "x1.25")
            {
                TimeScaleIndicator_label.Text = "x1.5";
                timer1.Interval = Convert.ToInt32(1000 / 1.5);
            }
            else if (TimeScaleIndicator_label.Text == "x1.5")
            {
                TimeScaleIndicator_label.Text = "x1.75";
                timer1.Interval = Convert.ToInt32(1000 / 1.75);
            }
            else if (TimeScaleIndicator_label.Text == "x1.75")
            {
                TimeScaleIndicator_label.Text = "x2";
                timer1.Interval = 1000 / 2;
            }
            else
            {
                MessageBox.Show("You can not move forward");
            }
        }

        private void TimeScaleBackward_button_Click(object sender, EventArgs e)
        {

            if (TimeScaleIndicator_label.Text == "x1")
            {
                TimeScaleIndicator_label.Text = "x0.75";
                timer1.Interval = Convert.ToInt32((1000) / 0.75);
            }
            else if (TimeScaleIndicator_label.Text == "x0.75")
            {
                TimeScaleIndicator_label.Text = "x0.5";
                timer1.Interval = Convert.ToInt32((1000) / 0.5);
            }
            else if (TimeScaleIndicator_label.Text == "x0.5")
            {
                TimeScaleIndicator_label.Text = "x0.25";
                timer1.Interval = Convert.ToInt32(1000 / 0.25);
            }
            else if (TimeScaleIndicator_label.Text == "x1.5")
            {
                TimeScaleIndicator_label.Text = "x1.25";
                timer1.Interval = Convert.ToInt32(1000 / 1.25);
            }
            else if (TimeScaleIndicator_label.Text == "x1.75")
            {
                TimeScaleIndicator_label.Text = "x1.5";
                timer1.Interval = Convert.ToInt32(1000 / 1.5);
            }
            else if (TimeScaleIndicator_label.Text == "x1.25")
            {
                TimeScaleIndicator_label.Text = "x1";
                timer1.Interval = 1000;
            }
            else if (TimeScaleIndicator_label.Text == "x2")
            {
                TimeScaleIndicator_label.Text = "x1.75";
                timer1.Interval = Convert.ToInt32(1000 / 1.75);
            }
            else
            {
                MessageBox.Show("You can not move backward");
            }

        }

        // CHECK BOXES
        private void SMR_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            this.SMR_overlay.IsVisibile = SMR_checkBox.Checked;
        }

        private void MLAT_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            this.MLAT_overlay.IsVisibile = MLAT_checkBox.Checked;
        }

        private void ADSB_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            this.ADSB_overlay.IsVisibile = ADSB_checkBox.Checked;
        }

        private void SeeTraces_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            this.SMR_traces_overlay.IsVisibile = SeeTraces_checkBox.Checked;
            this.MLAT_traces_overlay.IsVisibile = SeeTraces_checkBox.Checked;
            this.ADSB_traces_overlay.IsVisibile = SeeTraces_checkBox.Checked;
        }

        // SET HOUR
        private void Set_button_Click(object sender, EventArgs e)
        {
            try
            {
                if (Hour_comboBox.SelectedIndex != -1)
                {
                    ClearCurrentLists();

                    if (Minuts_comboBox.SelectedIndex == -1)
                        Minuts_comboBox.SelectedItem = "00";

                    if (Seconds_comboBox.SelectedIndex == -1)
                        Seconds_comboBox.SelectedItem = "00";

                    this.currentTime = Hour_comboBox.SelectedItem.ToString() + ":" + Minuts_comboBox.SelectedItem.ToString() + ":" + Seconds_comboBox.SelectedItem.ToString();
                    Time_label.Text = this.currentTime;

                    this.list_index = this.targetData_list.Count; // Just in case the following loop does not find the index value (because the Time Set is when the file with messages has already finished)
                    for (int index = 0; index < this.targetData_list.Count; index++)
                    {
                        if ((double)System.TimeSpan.Parse(targetData_list[index].Time).TotalSeconds >= (double)System.TimeSpan.Parse(this.currentTime).TotalSeconds)
                        {
                            this.list_index = index;
                            break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Select at least the start Hour!");
                }
            }
            catch
            {
                MessageBox.Show("An error has occurred.\nPlease try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // ZOOM IN LEBL
        private void ZoomInLEBL_button_Click(object sender, EventArgs e)
        {
            GMap_control.Position = new PointLatLng(41.403046, 2.162958);
            GMap_control.Position = LEBL_position;
            GMap_control.MinZoom = 8;
            GMap_control.MaxZoom = 22;
            GMap_control.Zoom = 13;
            GMap_control.AutoScroll = true;
        }

        // -----------------------------------------------------------------------------------------------------------------------------

        private void CheckTargets()
        {
            //if ((double)System.TimeSpan.Parse(this.currentTime).TotalSeconds % 5  == 0)
            //    ClearLists();
            ClearCurrentLists();
            for (; this.list_index<this.targetData_list.Count; this.list_index++)
            {
                if ((double)System.TimeSpan.Parse(targetData_list[this.list_index].Time).TotalSeconds <= (double)System.TimeSpan.Parse(this.currentTime).TotalSeconds)
                {
                    AddMarkerToItsOverlay(this.list_index);
                }
                else
                {
                    break;
                } 
            }

            // Check if we have reached the end of the simulation
            if (this.list_index == this.targetData_list.Count)
            {
                Stop_button.Hide();
                Play_button.Show();

                timerstarted = false;
                timer1.Stop();

                MessageBox.Show("End of the file has been reached!", "End", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddMarkerToItsOverlay(int index)
        {
            CoordinatesXYZ cartesian;
            CoordinatesXYZ geocentric = new CoordinatesXYZ();
            double latitude = 0;
            double longitude = 0;

            GMarkerGoogleType markerType = new GMarkerGoogleType();
            GMarkerGoogle marker;

            if (this.targetData_list[index].isSMR is true || this.targetData_list[index].isMLAT is true)
            {
                if (this.targetData_list[index].isSMR is true)
                {
                    cartesian = new CoordinatesXYZ(this.targetData_list[index].Position[0], this.targetData_list[index].Position[1], this.targetData_list[index].Position[2]);
                    geocentric = this.myGeoUtils.change_radar_cartesian2geocentric(this.SMR_radar_WGS84Coordinates, cartesian);
                    CoordinatesWGS84 geodesic = this.myGeoUtils.change_geocentric2geodesic(geocentric);
                    latitude = Functions.Rad2Deg(geodesic.Lat);
                    longitude = Functions.Rad2Deg(geodesic.Lon);
                    //double h = geodesic.Height;

                    markerType = GMarkerGoogleType.yellow_small;
                    marker = CreateMarker(latitude, longitude, markerType, index);

                    RemovePreviousMarker_and_AddNewMarker(marker, this.SMR_markerTags, this.SMR_overlay, this.SMR_traces_overlay, latitude,longitude);
                }
                else if (this.targetData_list[index].isMLAT is true)
                {
                    cartesian = new CoordinatesXYZ(this.targetData_list[index].Position[0], this.targetData_list[index].Position[1], this.targetData_list[index].Position[2]);
                    geocentric = this.myGeoUtils.change_radar_cartesian2geocentric(this.MLAT_radar_WGS84Coordinates, cartesian);
                    CoordinatesWGS84 geodesic = this.myGeoUtils.change_geocentric2geodesic(geocentric);
                    latitude = Functions.Rad2Deg(geodesic.Lat);
                    longitude = Functions.Rad2Deg(geodesic.Lon);
                    //double h = geodesic.Height;

                    markerType = GMarkerGoogleType.green_small;
                    marker = CreateMarker(latitude, longitude, markerType, index);

                    RemovePreviousMarker_and_AddNewMarker(marker, this.MLAT_markerTags, this.MLAT_overlay, this.MLAT_traces_overlay, latitude,longitude);
                }
            }
            else //if (this.targetData_list[index].isADSB is true)
            {
                latitude = this.targetData_list[index].Position[0];
                longitude = this.targetData_list[index].Position[1];

                markerType = GMarkerGoogleType.red_small;
                marker = CreateMarker(latitude, longitude, markerType, index);

                RemovePreviousMarker_and_AddNewMarker(marker, this.ADSB_markerTags, this.ADSB_overlay, this.ADSB_traces_overlay, latitude,longitude);
            }

        }

    private GMarkerGoogle CreateMarker(double latitude, double longitude, GMarkerGoogleType markerType, int index)
        {
            GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(latitude, longitude), markerType);
            marker.ToolTipText = this.targetData_list[index].ID[0];
            marker.Tag = index;

            return marker;
        }

        private void RemovePreviousMarker_and_AddNewMarker(GMarkerGoogle marker, List<string> markerTags, GMapOverlay overlay, GMapOverlay traces_overlay, double latitude,double longitude)
        {
            /*
            int delete_index = markerTags.IndexOf(marker.ToolTipText);
            if (delete_index != -1)
            {
                // Add removed marker to the traces overlay
                overlay.Markers[delete_index].Size = new Size(9, 9);
                traces_overlay.Markers.Add(overlay.Markers[delete_index]);
                

                // Remove previous marker
                markerTags.RemoveAt(delete_index);
                overlay.Markers.RemoveAt(delete_index);
            }
            */
            markerTags.Add(marker.ToolTipText);
            overlay.Markers.Add(marker);
        }

        private void Set_TargetData_DGV(GMap.NET.WindowsForms.GMapMarker target)
        {
            label2.Hide();

            TargetData_DGV.Columns.Clear();
            TargetData_DGV.Rows.Clear();

            TargetData_DGV.RowHeadersVisible = false;
            TargetData_DGV.ColumnHeadersVisible = false;

            // Set the number of rows and columns
            TargetData_DGV.RowCount = 8;
            TargetData_DGV.ColumnCount = 2;

            TargetData_DGV.Rows[0].Cells[0].Selected = false;
            TargetData_DGV.Columns[0].Width = 160;
            TargetData_DGV.Columns[1].Width = 160;

            // Column[0] bold
            TargetData_DGV.Columns[0].DefaultCellStyle.Font = new Font("Tahoma", 11, FontStyle.Bold);
            // Column[1] not bold
            TargetData_DGV.Columns[1].DefaultCellStyle.Font = new Font("Tahoma", 11);

            TargetData_DGV.Rows[0].Cells[0].Value = "Time";
            TargetData_DGV.Rows[1].Cells[0].Value = "Pos. Latitude";
            TargetData_DGV.Rows[2].Cells[0].Value = "Pos. Longitude";
            TargetData_DGV.Rows[3].Cells[0].Value = "Track Number";
            TargetData_DGV.Rows[4].Cells[0].Value = "Target Address";
            TargetData_DGV.Rows[5].Cells[0].Value = "Target Identification";
            TargetData_DGV.Rows[6].Cells[0].Value = "Mode 3A Code";
            TargetData_DGV.Rows[7].Cells[0].Value = "Flight Level";

            int index = (int)target.Tag;

            TargetData_DGV.Rows[0].Cells[1].Value = this.targetData_list[index].Time;
            TargetData_DGV.Rows[1].Cells[1].Value = target.Position.Lat;
            TargetData_DGV.Rows[2].Cells[1].Value = target.Position.Lng;

            string[] IDs = this.targetData_list[index].ID;
            for (int i = 0; i < IDs.Length; i++)
            {
                if (IDs[i] != null)
                    TargetData_DGV.Rows[i + 3].Cells[1].Value = IDs[i];
                else
                    TargetData_DGV.Rows[i + 3].Cells[1].Value = "--";

            }

            string mode3a = this.targetData_list[index].Mode3ACode;
            if (mode3a != null )
                TargetData_DGV.Rows[6].Cells[1].Value = mode3a;
            else
                TargetData_DGV.Rows[6].Cells[1].Value = "--";


            double FL = this.targetData_list[index].FlightLevel;
            if (FL != 0)  
                TargetData_DGV.Rows[7].Cells[1].Value = FL;
            else
                TargetData_DGV.Rows[7].Cells[1].Value = "--";
        }
 

        // ------------------------------------------------------
        // Clear lists methods
        private void ClearCurrentLists()
        {
            this.SMR_overlay.Clear();
            this.MLAT_overlay.Clear();
            this.ADSB_overlay.Clear();

            this.SMR_markerTags.Clear();
            this.MLAT_markerTags.Clear();
            this.ADSB_markerTags.Clear();
        }

        private void ClearTracesLists()
        {
            this.SMR_traces_overlay.Clear();
            this.MLAT_traces_overlay.Clear();
            this.ADSB_traces_overlay.Clear();
        }

        // ------------------------------------------------------
        // Loading Button State functions
        private void Loading_ButtonState(Button button)
        {
            button.Text = "Loading\n...";
            button.ForeColor = Color.White;
            button.BackColor = Color.Black;
            Application.DoEvents();
        }

        private void FinishedLoading_ButtonState(Button button, string str)
        {
            button.Text = str;
            button.ForeColor = Color.Black;
            button.BackColor = Color.White;
        }


    }
}
