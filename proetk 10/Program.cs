using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;


public class Contact
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }

    public Contact() { }

    public Contact(string name, string phoneNumber, string email, string address)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        Address = address;
    }
}

public class ContactChange
{
    public int ID { get; set; }
    public int ContactID { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DatabaseManager
{
    internal SQLiteConnection connection;

    public DatabaseManager()
    {
        connection = new SQLiteConnection("Data Source=phonebook.db;Version=3;");
        connection.Open();

        InitializeDatabase();
    }

    public void InitializeDatabase()
    {
        string createContactsTableQuery = @"
        CREATE TABLE IF NOT EXISTS Contacts (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            PhoneNumber TEXT,
            Email TEXT,
            Address TEXT
        );
    ";

        string createHistoryTableQuery = @"
        CREATE TABLE IF NOT EXISTS History (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            ContactID INTEGER,
            FieldName TEXT,
            OldValue TEXT,
            NewValue TEXT,
            Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY(ContactID) REFERENCES Contacts(ID)
        );
    ";

        using (SQLiteCommand command = new SQLiteCommand(createContactsTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }

        using (SQLiteCommand command = new SQLiteCommand(createHistoryTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }
    }


    public class ContactsManager
    {
        internal DatabaseManager dbManager;

        public ContactsManager(DatabaseManager manager)
        {
            dbManager = manager;
        }

        public void AddContact(Contact contact)
        {
            using (SQLiteCommand command = new SQLiteCommand(dbManager.connection))
            {
                command.CommandText = "INSERT INTO Contacts (Name, PhoneNumber, Email, Address) VALUES (@Name, @PhoneNumber, @Email, @Address)";
                command.Parameters.AddWithValue("Name", contact.Name);
                command.Parameters.AddWithValue("PhoneNumber", contact.PhoneNumber);
                command.Parameters.AddWithValue("Email", contact.Email);
                command.Parameters.AddWithValue("Address", contact.Address);
                command.ExecuteNonQuery();
            }
        }

        public void EditContact(int contactID, Contact newContact)
        {
            using (SQLiteCommand command = new SQLiteCommand(dbManager.connection))
            {
                command.CommandText = "UPDATE Contacts SET Name = @Name, PhoneNumber = @PhoneNumber, Email = @Email, Address = @Address WHERE ID = @ContactID";
                command.Parameters.AddWithValue("Name", newContact.Name);
                command.Parameters.AddWithValue("PhoneNumber", newContact.PhoneNumber);
                command.Parameters.AddWithValue("Email", newContact.Email);
                command.Parameters.AddWithValue("Address", newContact.Address);
                command.Parameters.AddWithValue("ContactID", contactID);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteContact(int contactID)
        {
            using (SQLiteCommand command = new SQLiteCommand(dbManager.connection))
            {
                command.CommandText = "DELETE FROM Contacts WHERE ID = @ContactID";
                command.Parameters.AddWithValue("ContactID", contactID);
                command.ExecuteNonQuery();
            }
        }

        public List<Contact> SearchContacts(string searchText)
        {
            List<Contact> contacts = new List<Contact>();

            using (SQLiteCommand command = new SQLiteCommand(dbManager.connection))
            {
                command.CommandText = "SELECT * FROM Contacts WHERE Name LIKE @SearchText";
                command.Parameters.AddWithValue("SearchText", "%" + searchText + "%");
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Contact contact = new Contact
                        {
                            ID = Convert.ToInt32(reader["ID"]),
                            Name = reader["Name"].ToString(),
                            PhoneNumber = reader["PhoneNumber"].ToString(),
                            Email = reader["Email"].ToString(),
                            Address = reader["Address"].ToString()
                        };
                        contacts.Add(contact);
                    }
                }
            }

            return contacts;
        }

        public List<ContactChange> GetContactHistory(int contactID)
        {
            List<ContactChange> history = new List<ContactChange>();

            using (SQLiteCommand command = new SQLiteCommand(dbManager.connection))
            {
                command.CommandText = "SELECT * FROM History WHERE ContactID = @ContactID";
                command.Parameters.AddWithValue("ContactID", contactID);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ContactChange change = new ContactChange
                        {
                            ID = Convert.ToInt32(reader["ID"]),
                            ContactID = Convert.ToInt32(reader["ContactID"]),
                            FieldName = reader["FieldName"].ToString(),
                            OldValue = reader["OldValue"].ToString(),
                            NewValue = reader["NewValue"].ToString(),
                            Timestamp = Convert.ToDateTime(reader["Timestamp"])
                        };
                        history.Add(change);
                    }
                }
            }

            return history;
        }
    }

    public class UserInterface
    {
        protected ContactsManager contactsManager;

        public UserInterface(ContactsManager manager)
        {
            contactsManager = manager;
        }

        public void Start()
        {
            Console.WriteLine("Phonebook Application");
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("Select an option:");
                Console.WriteLine("1. Add new contact");
                Console.WriteLine("2. Edit a contact");
                Console.WriteLine("3. Delete a contact");
                Console.WriteLine("4. Search contacts");
                Console.WriteLine("5. View contact history");
                Console.WriteLine("6. Exit");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddNewContact();
                        break;
                    case "2":
                        EditContact();
                        break;
                    case "3":
                        DeleteContact();
                        break;
                    case "4":
                        SearchContacts();
                        break;
                    case "5":
                        ViewContactHistory();
                        break;
                    case "6":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }
            }
        }

        private void AddNewContact()
        {
            Console.WriteLine("Enter contact details:");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Phone Number: ");
            string phoneNumber = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Address: ");
            string address = Console.ReadLine();

            try
            {
                Contact newContact = new Contact(name, phoneNumber, email, address);
                contactsManager.AddContact(newContact);
                Console.WriteLine("Contact added successfully.");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("Error adding the contact: " + ex.Message);
            }
            finally
            {

            }
        }

        private void EditContact()
        {
            Console.Write("Enter the ID of the contact you want to edit: ");
            if (int.TryParse(Console.ReadLine(), out int contactID))
            {
                Console.WriteLine("Enter new contact details:");

                Console.Write("Name: ");
                string name = Console.ReadLine();

                Console.Write("Phone Number: ");
                string phoneNumber = Console.ReadLine();

                Console.Write("Email: ");
                string email = Console.ReadLine();

                Console.Write("Address: ");
                string address = Console.ReadLine();

                try
                {
                    Contact newContact = new Contact(name, phoneNumber, email, address);
                    contactsManager.EditContact(contactID, newContact);
                    Console.WriteLine("Contact edited successfully.");
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine("Error editing the contact: " + ex.Message);
                }
                finally
                {

                }
            }
            else
            {
                Console.WriteLine("Invalid contact ID.");
            }
        }

        private void DeleteContact()
        {
            Console.Write("Enter the ID of the contact you want to delete: ");
            if (int.TryParse(Console.ReadLine(), out int contactID))
            {
                try
                {
                    contactsManager.DeleteContact(contactID);
                    Console.WriteLine("Contact deleted successfully.");
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine("Error deleting the contact: " + ex.Message);
                }
                finally
                {

                }
            }
            else
            {
                Console.WriteLine("Invalid contact ID.");
            }
        }

        private void SearchContacts()
        {
            Console.Write("Enter a search term: ");
            string searchText = Console.ReadLine();
            try
            {
                List<Contact> searchResults = contactsManager.SearchContacts(searchText);

                Console.WriteLine("Search results:");
                foreach (Contact contact in searchResults)
                {
                    Console.WriteLine($"ID: {contact.ID}, Name: {contact.Name}, Phone: {contact.PhoneNumber}, Email: {contact.Email}, Address: {contact.Address}");
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("Error searching for contacts: " + ex.Message);
            }
            finally
            {
                // Clean up resources or perform any necessary actions.
            }
        }

        private void ViewContactHistory()
        {
            Console.Write("Enter the ID of the contact for which you want to view history: ");
            if (int.TryParse(Console.ReadLine(), out int contactID))
            {
                try
                {
                    List<ContactChange> history = contactsManager.GetContactHistory(contactID);

                    Console.WriteLine("Contact history:");
                    foreach (ContactChange change in history)
                    {
                        Console.WriteLine($"Change ID: {change.ID}, Field: {change.FieldName}, Old Value: {change.OldValue}, New Value: {change.NewValue}, Timestamp: {change.Timestamp}");
                    }
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine("Error viewing contact history: " + ex.Message);
                }
                finally
                {

                }
            }
            else
            {
                Console.WriteLine("Invalid contact ID.");
            }
        }
    }


    public class Program
    {
        public static void Main()
        {
            DatabaseManager databaseManager = new DatabaseManager();
            ContactsManager contactsManager = new ContactsManager(databaseManager);
            UserInterface userInterface = new UserInterface(contactsManager);
            userInterface.Start();
        }
    }
}