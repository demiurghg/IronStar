using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace Fusion.Development {
	public partial class ArgumentDialog : Form {

		public static object[] Show ( IWin32Window owner, string methodNiceName, ParameterInfo[] args )
		{
			var argDialog = new ArgumentDialog(args);

			argDialog.Text				=	methodNiceName;

			var r = argDialog.ShowDialog( owner );

			if (r==DialogResult.OK) {

				

				return new object[0];
			}

			return null;
		}
		

		private ArgumentDialog( ParameterInfo[] args )
		{
			InitializeComponent();

			ClientSize = new Size(330, args.Length * 28 + 20 + 50);

			int ypos = -10;

			foreach ( var arg in args ) {

				ypos += 28;

				if (arg.ParameterType!=typeof(bool)) {
					var label = new Label();
					label.AutoSize = false;
					label.Text = MakeNiceName( arg.Name );
					label.TextAlign = ContentAlignment.MiddleRight;
				
					label.Width = 100;
					label.Height = 20; 
					label.Location = new Point( 10, ypos );

					Controls.Add( label );
				}

				if (arg.ParameterType==typeof(bool)) {
					
					var checkbox = new CheckBox();

					checkbox.AutoSize = false;
					checkbox.Text = MakeNiceName( arg.Name );
					checkbox.TextAlign = ContentAlignment.MiddleLeft;

					checkbox.Checked = arg.RawDefaultValue==DBNull.Value? false : (bool)(arg.DefaultValue);
				
					checkbox.Width = 200;
					checkbox.Height = 20; 
					checkbox.Location = new Point( 120, ypos );

					Controls.Add( checkbox );
				}

				if (arg.ParameterType==typeof(string)) {
					
					var textbox = new TextBox();

					textbox.AutoSize = false;
					textbox.Text = (arg.DefaultValue as string) ?? "";
					textbox.TextAlign = HorizontalAlignment.Left;
				
					textbox.Width = 180;
					textbox.Height = 20; 
					textbox.Location = new Point( 120, ypos );

					Controls.Add( textbox );
				}

				if (arg.ParameterType.IsEnum) {
					
					var comboBox = new ComboBox();

					comboBox.AutoSize = false;
					comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
					comboBox.Items.AddRange( Enum.GetValues( arg.ParameterType ).Cast<object>().ToArray() );
					comboBox.SelectedIndex = 0;
				
					comboBox.Width = 180;
					comboBox.Height = 20; 
					comboBox.Location = new Point( 120, ypos );

					Controls.Add( comboBox );
				}
			}
		}


		
		string MakeNiceName ( string text )
		{
			if (string.IsNullOrEmpty(text)) {
				return "";
			}

			var sb = new StringBuilder();

			sb.Append( char.ToUpperInvariant(text[0]) );

			for ( int i=1; i<text.Length; i++ ) {
				if (char.IsLower(text[i-1]) && char.IsUpper(text[i])) {
					sb.Append(' ');
				}
				sb.Append(text[i]);
			}

			return sb.ToString();
		}



		private void buttonOK_Click( object sender, EventArgs e )
		{
			this.DialogResult = DialogResult.OK;
		}

		private void buttonCancel_Click( object sender, EventArgs e )
		{
			this.DialogResult = DialogResult.Cancel;
		}
	}
}
