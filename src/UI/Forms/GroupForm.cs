﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WebsiteStudio.Core;
using WebsiteStudio.Core.Security;
using WebsiteStudio.Core.Validation;
using WebsiteStudio.UI.Localization;
using WebsiteStudio.UI.Resources;

namespace WebsiteStudio.UI.Forms {
	public partial class GroupForm : Form {

		private readonly Group _Group;
		private readonly Project _Project;
		private readonly GroupValidator.Mode _ValidationMode = GroupValidator.Mode.Edit;
		private readonly List<User> _Members;

		public Group Group => _Group;

		public GroupForm(Project project)
			: this(project, project.CreateGroup()) {

			_ValidationMode = GroupValidator.Mode.Add;
		}

		public GroupForm(Project project, Group group) {
			InitializeComponent();
			LocalizeComponent();

			DialogResult = DialogResult.Cancel;
			Icon = IconPack.Current.GetIcon(Interface.Icons.IconPackIcon.Group);

			_Project = project;
			_Group = group;
			txtName.Text = _Group.Name;

			_Members = new List<User>();
			_Members.AddRange(_Project.Users.Where(user => user.Memberships.Contains(_Group)));
			lvwMembers.VirtualListSize = _Members.Count;
		}

		private void LocalizeComponent() {
			Text = Strings.Group;
			tabGeneral.Text = Strings.General;
			tabMembers.Text = Strings.Members;

			lblName.Text = Strings.Name + ":";

			clnMemberName.Text = Strings.Name;

			btnAccept.Text = Strings.Accept;
			btnCancel.Text = Strings.Cancel;
		}

		private void ApplyToGroup(Group group) {
			group.Name = txtName.Text;
		}
		
		private void btnAccept_Click(object sender, EventArgs e) {

			Group validationGroup = _Project.CreateGroup();
			ApplyToGroup(validationGroup);

			ValidationHelper<Group> validator = new ValidationHelper<Group>(new GroupValidator(validationGroup, _Project, _ValidationMode));
			if (!validator.Valid) {
				validator.ShowMessage();
				return;
			}

			ApplyToGroup(_Group);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e) {
			Close();
		}

		private void lvwMembers_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e) {
			User user = _Members[e.ItemIndex];
			e.Item = new ListViewItem(user.Name);
		}
	}
}
