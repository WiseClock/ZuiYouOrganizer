namespace ZuiYouNameOrganizer
{
    class User
    {
        public string ID { get; private set; }
        public string Username { get; private set; }

        public User(string username, string id)
        {
            Username = username;
            ID = id;
        }
    }
}
