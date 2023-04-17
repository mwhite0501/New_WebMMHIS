using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace NewMMHIS_Web.Controllers
{
    public class Interop
    {
        public delegate void LatLngEventHandler(double latitude, double longitude);
        public static event LatLngEventHandler OnLatLngReceived;

        [JSInvokable]
        public static void UpdateMap2(double latitude, double longitude)
        {
            OnLatLngReceived?.Invoke(latitude, longitude);
        }
    }

}
