using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace PW_Manager
{
    public partial class MainForm : Form
    {
        private string masterPassword;
        private const string dataFile = "data.dat";

        private List<Account> accountList = new List<Account>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (PasswordForm passwordForm = new PasswordForm())
            {
                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    masterPassword = passwordForm.Password;
                    if (!File.Exists(dataFile))
                    {
                        File.Create(dataFile);
                    }
                    else
                    {
                        if (!IsFileEmpty(dataFile))
                        {
                            loadData();
                        }
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ReadDataGridView();
            saveData();
        }

        private void ReadDataGridView()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                string accountName = row.Cells[0].Value?.ToString();
                string password = row.Cells[1].Value?.ToString();
                string description = row.Cells[2].Value?.ToString();

                if (string.IsNullOrEmpty(accountName) && string.IsNullOrEmpty(password))
                {
                    continue;
                }

                Account newAccount = new Account
                {
                    AccountName = accountName,
                    Password = password,
                    Description = description
                };

                accountList.Add(newAccount);
            }
        }

        private void saveData()
        {
            try
            {
                string serializedData = SerializeData(accountList);
                string encryptedData = EncryptData(serializedData, masterPassword);

                File.WriteAllText(dataFile, encryptedData);
                MessageBox.Show("Erfolgreich gespeichert");
                accountList.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern der Daten: " + ex.Message);
            }
        }

        private void loadData()
        {
            try
            {
                string encryptedData = File.ReadAllText(dataFile);

                string decryptedData = DecryptData(encryptedData, masterPassword);

                accountList = DeserializeData(decryptedData);

                PopulateDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Daten: " + ex.Message);
            }
        }

        private void PopulateDataGridView()
        {
            dataGridView1.Rows.Clear();

            foreach (var account in accountList)
            {
                dataGridView1.Rows.Add(account.AccountName, account.Password, account.Description);
            }
            accountList.Clear();
        }

        private string SerializeData(List<Account> data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, data);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private List<Account> DeserializeData(string data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            byte[] bytes = Convert.FromBase64String(data);
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return (List<Account>)formatter.Deserialize(ms);
            }
        }

        private string EncryptData(string data, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(key ?? ""), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 1000);

                aesAlg.Key = keyDerivation.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = keyDerivation.GetBytes(aesAlg.BlockSize / 8);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptData(string data, string key)
        {
            byte[] encryptedData = Convert.FromBase64String(data);

            using (Aes aesAlg = Aes.Create())
            {
                Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(key ?? ""), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 1000);

                aesAlg.Key = keyDerivation.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = keyDerivation.GetBytes(aesAlg.BlockSize / 8);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        static bool IsFileEmpty(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length == 0;
            }
            else
            {
                return true;
            }
        }
    }

    [Serializable]
    public class Account
    {
        public string AccountName { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }
}