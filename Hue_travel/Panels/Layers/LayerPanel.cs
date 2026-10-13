using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.Shared;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Layers
{
    /// <summary>
    /// "Lớp dữ liệu": the places of the group picked in the filter bar (or all of them), each with a switch, and one switch for
    /// the whole list. A switched-off place leaves the map until it is switched on again.
    /// </summary>
    public sealed class LayerPanel : FloatingPanelForm
    {
        private readonly PanelHeader header = new PanelHeader { IconName = "layers", Text = "Lớp dữ liệu", Prominent = true };
        private readonly Panel top = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = ThemeColors.Slate50 };
        private readonly Label lblTitle = new Label { AutoSize = false, Font = ThemeFonts.Bold(18), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, 12), Size = new Size(350, 30) };
        private readonly Label lblCount = new Label { AutoSize = false, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, 42), Size = new Size(350, 20) };
        private readonly Label lblAll = new Label { AutoSize = false, Font = ThemeFonts.SemiBold(13), ForeColor = ThemeColors.Slate700, TextAlign = ContentAlignment.MiddleLeft, Text = "Hiện tất cả trên bản đồ", Location = new Point(16, 76), Size = new Size(280, 28) };
        private readonly ToggleSwitch masterSwitch = new ToggleSwitch { Location = new Point(322, 78) };
        private readonly PoiRowList list = new PoiRowList { Dock = DockStyle.Fill, ShowSwitch = true };
        private readonly HashSet<string> hidden = new HashSet<string>();
        private PoiCategory? category;
        private bool updatingSwitch;

        /// <summary>Raised after any switch changed; <see cref="HiddenIds"/> then holds what the map must leave out.</summary>
        public event EventHandler HiddenChanged;

        /// <summary>Raised when a place is clicked (not its switch).</summary>
        public event EventHandler<Poi> PoiSelected;

        public LayerPanel()
        {
            Size = new Size(384, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            lblAll.Cursor = Cursors.Hand;
            lblAll.Click += (s, e) => masterSwitch.Toggle();
            top.Controls.Add(lblTitle);
            top.Controls.Add(lblCount);
            top.Controls.Add(lblAll);
            top.Controls.Add(masterSwitch);
            masterSwitch.CheckedChanged += (s, e) => { if (!updatingSwitch) SetAll(masterSwitch.Checked); };

            list.RowClicked += (s, row) => PoiSelected?.Invoke(this, row.Poi);
            list.SwitchToggled += (s, row) =>
            {
                if (row.On) hidden.Remove(row.Poi.Id); else hidden.Add(row.Poi.Id);
                RefreshSummary();
                HiddenChanged?.Invoke(this, EventArgs.Empty);
            };

            Controls.Add(list);
            Controls.Add(top);
            Controls.Add(header);
            PoiCatalog.Changed += (s, e) => { if (!IsDisposed) Reload(); };
        }

        public IReadOnlyCollection<string> HiddenIds => hidden;

        /// <summary>Lists the places of one group (null = every group).</summary>
        public void SetCategory(PoiCategory? value)
        {
            category = value;
            Reload();
        }

        public void Reload()
        {
            IEnumerable<Poi> places = PoiCatalog.All.Where(p => (p.X != 0 || p.Y != 0) && (category == null || p.Category == category.Value));
            var rows = places.OrderBy(p => p.Category).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => new PoiRow(p) { On = !hidden.Contains(p.Id), Subtitle = SubtitleOf(p) }).ToList();
            list.EmptyText = "Chưa có địa điểm nào trong nhóm này.";
            list.SetRows(rows);
            lblTitle.Text = category == null ? "Tất cả địa điểm" : PoiMarkerIcons.NameOf(category.Value);
            RefreshSummary();
        }

        private static string SubtitleOf(Poi poi) =>
            PoiMarkerIcons.Describe(poi);

        private void SetAll(bool on)
        {
            for (int i = 0; i < list.RowCount; i++)
            {
                PoiRow row = list.RowAt(i);
                row.On = on;
                if (on) hidden.Remove(row.Poi.Id); else hidden.Add(row.Poi.Id);
            }
            list.Repaint();
            RefreshSummary();
            HiddenChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshSummary()
        {
            int total = list.RowCount, shown = 0;
            for (int i = 0; i < total; i++) if (list.RowAt(i).On) shown++;
            lblCount.Text = total == 0 ? "Không có địa điểm" : shown + " / " + total + " địa điểm đang hiện trên bản đồ";
            updatingSwitch = true;
            masterSwitch.SetState(shown == total && total > 0, shown > 0 && shown < total);
            updatingSwitch = false;
        }
    }
}
