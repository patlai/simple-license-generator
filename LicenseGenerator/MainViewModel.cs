using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace LicenseGenerator
{
	class MainViewModel : INotifyPropertyChanged
	{

		private string _privateKey = "some random private key string";

		private string _userName;

		public string UserName
		{
			get { return _userName; }
			set { _userName = value; OnPropertyChanged("UserName"); }
		}

		private string _email;

		public string Email
		{
			get { return _email; }
			set { _email = value; OnPropertyChanged("Email"); }
		}

		private string _generatedKey;

		public string GeneratedKey
		{
			get { return _generatedKey; }
			set { _generatedKey = value; OnPropertyChanged("GeneratedKey"); }
		}

		private DateTime _expirationDate;

		public DateTime ExpirationDate
		{
			get { return _expirationDate; }
			set { _expirationDate = value; OnPropertyChanged("ExpirationDate"); }
		}



		private void GenerateUserLicense(string userName, string email, string key)
		{
			var hash = GetSha256Hash(userName + email + key);
			var license = new string(ToHexString(hash).Where((value, index) => index < 16).ToArray());
			GeneratedKey = license;
		}

		private void InsertNewUser(string userName, string email, string license, DateTime expiration)
		{
			try
			{
				string connectionString = "datasource=127.0.0.1;port=3306;username=root;password=;database=licensetable;";

				string selectQuery = "SELECT * FROM userkeys";

				string insertQuery =
					string.Format(
						"INSERT INTO `licensetable`.`userkeys` (`username`, `email`, `licensekey`, `expiration`) VALUES ('{0}', '{1}', '{2}', '{3}');",
						userName, email, license, string.Format("{0}-{1}-{2}", expiration.Year, expiration.Month, expiration.Day));

				MySqlConnection databaseConnection = new MySqlConnection(connectionString);
				MySqlCommand commandDatabase = new MySqlCommand(insertQuery, databaseConnection);
				commandDatabase.CommandTimeout = 60;

				MySqlDataReader MyReader2;
				databaseConnection.Open();
				MyReader2 = commandDatabase.ExecuteReader();     // Here the query will be executed and data saved into the database.  

				while (MyReader2.Read())
				{
				}


				databaseConnection.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}



		}

		private void CheckLicence(string[] userInfo, DateTime currentDate, string licenseHash)
		{
		
			string connectionString = "datasource=127.0.0.1;port=3306;username=root;password=;database=licensetable;";

			string query = "SELECT * FROM userkeys";

			// Prepare the connection
			MySqlConnection databaseConnection = new MySqlConnection(connectionString);
			MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection);
			commandDatabase.CommandTimeout = 60;
			MySqlDataReader reader;


			try
			{
				// Open the database
				databaseConnection.Open();

				// Execute the query
				reader = commandDatabase.ExecuteReader();



				if (reader.HasRows)
				{
					while (reader.Read())
					{
						//row structure: username, email, key, expiration date

						string[] row = { reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3) };
						if (row[0] == userInfo[0] && row[1] == userInfo[1])
						{
							var expDate = Convert.ToDateTime(row[3]);
							//date comparisson is less than zero if the first date is earlier
							if (row[2] == licenseHash && DateTime.Compare(currentDate, expDate) < 0)
							{
								Console.WriteLine("license okay");
							}
						}
					}
				}
				else
				{
					Console.WriteLine("No rows found.");
				}

				databaseConnection.Close();
			}
			catch (Exception ex)
			{
				// Show any error message.
				Console.WriteLine(ex.Message);
			}
		}

		private byte[] GetSha256Hash(string s)
		{

			SHA256 mySHA256 = SHA256Managed.Create();

			byte[] bytes = Encoding.UTF8.GetBytes(s);

			var hashvalue = mySHA256.ComputeHash(bytes);

			return hashvalue;
		}



		private string ToHexString(byte[] bytes)
		{
			var chars = new char[bytes.Length * 2];

			//chars[0] = '0';
			//chars[1] = 'x';

			for (int i = 0; i < bytes.Length; i++)
			{
				chars[2 * i] = ToHexDigit(bytes[i] / 16);
				chars[2 * i + 1] = ToHexDigit(bytes[i] % 16);
			}

			return new string(chars);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}


		private char ToHexDigit(int i)
		{
			if (i < 10)
				return (char)(i + '0');
			else
				return (char)(i - 10 + 'A');
		}

		private ICommand _generateKeyCommand;

		public ICommand GenerateKeyCommand
		{
			get
			{
				if (_generateKeyCommand == null)
				{
					_generateKeyCommand = new RelayCommand(
							p => true,
							p => GenerateUserLicense(UserName, Email, _privateKey));
				}
				return _generateKeyCommand;
			}
		}

		private ICommand _addKeyCommand;

		public ICommand AddKeyCommand
		{
			get
			{
				if (_addKeyCommand == null)
				{
					_addKeyCommand = new RelayCommand(
							p => true,
							p => InsertNewUser(UserName, Email, GeneratedKey, ExpirationDate));
				}
				return _addKeyCommand;
			}
		}

	}
}
