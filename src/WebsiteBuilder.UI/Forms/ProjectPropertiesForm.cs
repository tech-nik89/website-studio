﻿using System;
using System.Windows.Forms;
using WebsiteBuilder.Core;
using WebsiteBuilder.UI.Localization;

namespace WebsiteBuilder.UI.Forms {
    public partial class ProjectPropertiesForm : Form {

        private readonly Project _Project;

        public ProjectPropertiesForm(Project project) {
            InitializeComponent();
            LocalizeComponent();

            _Project = project;

            plsLanguages.Languages = _Project.Languages;
            pgsGeneral.FillFromProject(_Project);

            DialogResult = DialogResult.Cancel;
        }

        private void LocalizeComponent() {
            Text = Strings.ProjectSettings;

            tabGeneral.Text = Strings.General;
            tabLanguages.Text = Strings.Languages;

            btnAccept.Text = Strings.Accept;
            btnCancel.Text = Strings.Cancel;
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            Close();
        }

        private void btnAccept_Click(object sender, EventArgs e) {
            _Project.Languages = plsLanguages.Languages;
            pgsGeneral.FillProjectFrom(_Project);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
