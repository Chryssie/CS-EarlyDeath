using ICities;

namespace EarlyDeath
{
    public class Identity : IUserMod
    {
        public string Name
        {
            get { return Settings.Instance.Tag; }
        }

        public string Description
        {
            get { return "Nonpermanently adds the possibility for your CIMs to die early. Disable to get the normal perfect life back. Requires [ARIS] Skylines Overwatch."; }
        }
    }
}