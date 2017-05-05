﻿using System;
using System.Windows.Forms;
using WebsiteBuilder.Core;
using WebsiteBuilder.Interface.Icons;
using WebsiteBuilder.UI.Localization;
using WebsiteBuilder.UI.Resources;

namespace WebsiteBuilder.UI.Forms {
    public partial class MainForm : Form {

        public MainForm() {
            InitializeComponent();
            LocalizeComponent();
            ApplyIcons();

            _ProductName = GetProductName();
            ofdProject.Filter = _ProjectFilesFilter;
            sfdProject.Filter = _ProjectFilesFilter;

            tslStatus.Text = StatusText.Ready;
            CurrentProject = new Project();
        }

        private void ApplyIcons() {
            if (IconPack.Current == null) {
                return;
            }

            mnuProjectSave.Image = IconPack.Current.GetImage(IconPackIcon.Save);
            mnuProjectOpen.Image = IconPack.Current.GetImage(IconPackIcon.Open);
            mnuProjectSettings.Image = IconPack.Current.GetImage(IconPackIcon.Settings);
            mnuProjectExit.Image = IconPack.Current.GetImage(IconPackIcon.Close);

            mnuContentMedia.Image = IconPack.Current.GetImage(IconPackIcon.Media);
            mnuContentFooter.Image = IconPack.Current.GetImage(IconPackIcon.Footer);

            mnuBuildProject.Image = IconPack.Current.GetImage(IconPackIcon.Build);

            tsbProjectNew.Image = IconPack.Current.GetImage(IconPackIcon.New);
            tsbProjectOpen.Image = IconPack.Current.GetImage(IconPackIcon.Open);
            tsbProjectSave.Image = IconPack.Current.GetImage(IconPackIcon.Save);

            tsbContentMedia.Image = IconPack.Current.GetImage(IconPackIcon.Media);
            tsbContentFooter.Image = IconPack.Current.GetImage(IconPackIcon.Footer);

            tsbBuildProject.Image = IconPack.Current.GetImage(IconPackIcon.Build);
        }

        private void LocalizeComponent() {
            mnuProject.Text = Strings.Project;
            mnuProjectNew.Text = Strings.New;
            mnuProjectOpen.Text = Strings.Open;
            mnuProjectSave.Text = Strings.Save;
            mnuProjectSaveAs.Text = Strings.SaveAs;
            mnuProjectSettings.Text = Strings.ProjectSettings;
            mnuProjectExit.Text = Strings.Exit;

            mnuContent.Text = Strings.Content;
            mnuContentMedia.Text = Strings.Media;
            mnuContentFooter.Text = Strings.Footer;

            mnuBuild.Text  = Strings.Build;
            mnuBuildProject.Text = Strings.BuildProject;
            mnuBuildAndRunProject.Text = Strings.BuildAndOpenProject;
            mnuBuildCleanOutput.Text = Strings.ClearOutputDirectory;

            tsbProjectNew.Text = Strings.New;
            tsbProjectOpen.Text = Strings.Open;
            tsbProjectSave.Text = Strings.Save;

            tsbContentMedia.Text = Strings.Media;
            tsbContentFooter.Text = Strings.Footer;

            tsbBuildProject.Text = Strings.Build;
        }

        private void mnuProjectExit_Click(object sender, EventArgs e) {
            Close();
        }
        
        private void mnuProjectSettings_Click(object sender, EventArgs e) {
            ProjectPropertiesForm form = new ProjectPropertiesForm(CurrentProject);
            DialogResult result = form.ShowDialog();

            if (result == DialogResult.OK) {
                ptvwPages.RefreshLanguageList();
                UpdateFormText();
            }
        }

        private void mnuProjectNew_Click(object sender, EventArgs e) {
            CurrentProject = new Project();
            UpdateFormText();
        }

        private void mnuProjectOpen_Click(object sender, EventArgs e) {
            OpenProject();
        }

        private void mnuProjectSave_Click(object sender, EventArgs e) {
            SaveProject(false);
        }

        private void mnuProjectSaveAs_Click(object sender, EventArgs e) {
            SaveProject(true);
        }

        private void mnuBuildProject_Click(object sender, EventArgs e) {
            CompileProject();
        }

        private void mnuBuildAndRunProject_Click(object sender, EventArgs e) {
            CompileProject(true);
        }

        private void mnuContentMedia_Click(object sender, EventArgs e) {
            if (CurrentProject == null) {
                return;
            }

            MediaForm form = new MediaForm(CurrentProject);
            form.ShowDialog();

            UpdateFormText();
        }

        private void mnuContentFooter_Click(object sender, EventArgs e) {
            if (CurrentProject == null) {
                return;
            }

            FooterContentForm form = new FooterContentForm(CurrentProject);
            form.ShowDialog();

            UpdateFormText();
        }

        private void ptvwPages_ContentUpdated(object sender, EventArgs e) {
            UpdateFormText();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            e.Cancel = ConfirmCloseDirtyProject();
        }

        private void mnuBuildCleanOutput_Click(object sender, EventArgs e) {
            ClearOutputDirectory();
        }
    }
}
