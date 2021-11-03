
window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
};

var imgInput = document.getElementById("Image")
        //var map;

        //    function initMap() {
        //        const myLatlng = {lat: 34.799999, lng: -92.199997 };
        //        const map = new google.maps.Map(document.getElementById("map"), {
        //    zoom: 7,
        //            center: myLatlng,
        //        });
        //        // Create the initial InfoWindow.
        //        let infoWindow = new google.maps.InfoWindow({
        //    content: "Click the map to get Lat/Lng!",
        //            position: myLatlng,
        //        });

        //        infoWindow.open(map);
        //        // Configure the click listener.
        //        map.addListener("click", (mapsMouseEvent) => {
        //    // Close the current InfoWindow.
        //    infoWindow.close();
        //            // Create a new InfoWindow.
        //            infoWindow = new google.maps.InfoWindow({
        //    position: mapsMouseEvent.latLng,
        //            });
        //            infoWindow.setContent(
        //                JSON.stringify(mapsMouseEvent.latLng.toJSON(), null, 2)
        //            );
        //            infoWindow.open(map);
        //            var test = JSON.stringify(mapsMouseEvent.latLng.toJSON(), null, 2);
        //            sendLatLng(test);
        //        });

        //    }
        //    //============================================================================
        //    function sendLatLng(test) {
        //        alert("TEST");
        //      $.ajax({
        //        url: '/api/Mmhis/Tes',
        //        type: "POST",
        //        data: { "latlong": test },
        //        success: function (mydata) {
        //            alert("Found");
        //        },
        //        error: function (error) {
        //            alert('Failed');
        //            alert(error);
        //        }
        //    });
        //    }
