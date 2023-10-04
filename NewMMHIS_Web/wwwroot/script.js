//window.enterFullScreen = () => { //this makes the entire webpage fullscreen, which might be useful down the line
//    const element = document.documentElement;

//    if (element.requestFullscreen) {
//        element.requestFullscreen();
//    } else if (element.mozRequestFullScreen) {
//        element.mozRequestFullScreen();
//    } else if (element.webkitRequestFullscreen) {
//        element.webkitRequestFullscreen();
//    } else if (element.msRequestFullscreen) {
//        element.msRequestFullscreen();
//    }
//};

//window.exitFullScreen = () => {
//    if (document.exitFullscreen) {
//        document.exitFullscreen();
//    } else if (document.mozCancelFullScreen) {
//        document.mozCancelFullScreen();
//    } else if (document.webkitExitFullscreen) {
//        document.webkitExitFullscreen();
//    } else if (document.msExitFullscreen) {
//        document.msExitFullscreen();
//    }
//};

//===================================================================================================================================
//=============================================Google Javascript API=================================================================
//===================================================================================================================================
//let dotNetReference;

//var map, marker;
//function initMap() {
//    // Create a map object and specify the DOM element for display.
//    map = new google.maps.Map(document.getElementById('google-map'), {
//        center: { lat: 34.751354, lng: -92.274592 }, // Set initial center coordinates
//        zoom: 8, // Set initial zoom level
//    });

//    // Create a marker and set its initial position
//    marker = new google.maps.Marker({
//        map: map,
//        position: { lat: 34.751354, lng: -92.274592 },
//        draggable: false, // Make the marker draggable if needed
//    });

//    google.maps.event.addListener(map, 'click', function (event) {
//        var clickedLocation = {
//            lat: event.latLng.lat(),
//            lng: event.latLng.lng()
//        };
//        console.log("Map clicked at: ", clickedLocation);  // Log to console
//        dotNetReference.invokeMethodAsync('AsyncMapOperationFromJS', clickedLocation);
//    });
//}

////function registerBlazorReference(reference) {
////    dotNetReference = reference;
////}

//// This function updates the marker's position
//function updateMarkerPosition(lat, lng) {
//    if (marker) {
//        var newLatLng = new google.maps.LatLng(lat, lng);
//        marker.setPosition(newLatLng);
//        map.setCenter(newLatLng); // if you want to keep the marker centered as it moves
//    }
//}
//===================================================================================================================================
//===================================================================================================================================
//===================================================================================================================================




window.ToggleFullScreen = function (elementId) {
    var element = document.getElementById(elementId);

    if (document.fullscreenElement || document.mozFullScreenElement || document.webkitFullscreenElement || document.msFullscreenElement) {
        // Exit full-screen mode
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.mozCancelFullScreen) {
            document.mozCancelFullScreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    } else {
        // Enter full-screen mode
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    }
};

window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
};

function submitForm() {
    var form = document.getElementById('mySubmitForm');
    if (form) {
        form.dispatchEvent(new Event('submit', { cancelable: true }));
    }
}

/* Set the width of the sidebar to 250px and the left margin of the page content to 250px */
function openNav() {
    document.getElementById("mySidebar").style.width = "250px";
    document.getElementById("main").style.marginLeft = "250px";
}

/* Set the width of the sidebar to 0 and the left margin of the page content to 0 */
function closeNav() {
    document.getElementById("mySidebar").style.width = "0";
    document.getElementById("main").style.marginLeft = "0";
}
function Clear() {
    document.getElementById("mySidebar").reset();
}

setTitle = (title) => { document.title = title; };

//async function loadImage(imageUrl) {
//    let img;
//    const imageLoadPromise = new Promise(resolve => {
//        img = new Image();
//        img.onload = resolve;
//        img.src = imageUrl;
//    });

//    await imageLoadPromise;
//    console.log("image loaded");
//    return img;
//}

//function preloadImages(array) {
//    if (!preloadImages.list) {
//        preloadImages.list = [];
//    }
//    var list = preloadImages.list;
//    for (var i = 0; i < array.length; i++) {
//        var img = new Image();
//        img.onload = function () {
//            var index = list.indexOf(this);
//            if (index !== -1) {
//                // remove image from the array once it's loaded
//                // for memory consumption reasons
//                list.splice(index, 1);
//            }
//        }
//        list.push(img);
//        img.src = array[i];
//    }
//}
window.ReloadPage = () => {
    location.reload();
}

function isDevice() {
    return [
        'iPad Simulator',
        'iPhone Simulator',
        'iPod Simulator',
        'iPad',
        'iPhone',
        'iPod'
    ].includes(navigator.platform)
        // iPad on iOS 13 detection
        || (navigator.userAgent.includes("Mac") && "ontouchend" in document)
}

function preventDefault(element) {
    element.addEventListener("wheel", function (event) {
        event.preventDefault();
    }, { passive: false });
}

//===================================================================================================================================
//=============================================ArcGIS Javascript API=================================================================
//===================================================================================================================================
let dotNetReference = null;

function setDotNetReference(reference) {
    dotNetReference = reference;
}

// Initialize variables globally
var map, graphicsLayer, Point;

let graphic;  // Define these outside the function for broader scope access
let view;

var initialized = false;

function setMap() {
    require([
        "esri/config",
        "esri/Map",
        "esri/views/MapView",
        "esri/layers/FeatureLayer",
        "esri/widgets/Measurement",
        "esri/layers/GraphicsLayer",
        "esri/geometry/Point",
        "esri/symbols/SimpleMarkerSymbol",
        "esri/Graphic",
        "esri/widgets/Legend",
    ], function (esriConfig, Map, MapView, FeatureLayer, Measurement, GraphicsLayer, Point, SimpleMarkerSymbol, Graphic, Legend) {
        esriConfig.apiKey = "";

        let featureLayer;

        const graphicsLayer = new GraphicsLayer();

        const map = new Map({
            basemap: "streets-navigation-vector",
            layers: [graphicsLayer]
        });

        const basemapSelect = document.getElementById("basemap-select");

        basemapSelect.addEventListener("change", (event) => {
            map.basemap = event.target.value;
        });

        view = new MapView({
            container: "viewDiv",
            map: map,
            zoom: 7
        });

        const featureLayerSelect = document.getElementById("feature-layer-select");

        featureLayerSelect.addEventListener("change", (event) => {
            // Remove the previous feature layer
            map.remove(featureLayer);

            // Create a new feature layer based on the selected URL
            featureLayer = new FeatureLayer({
                url: event.target.value,
                outFields: ["*"]
            });

            // Find the index of the graphicsLayer
            let graphicsLayerIndex = map.layers.indexOf(graphicsLayer);

            // Add the new feature layer to the map right below the graphicsLayer
            map.add(featureLayer, graphicsLayerIndex);
        });

        view.on("double-click", function (event) {
            // Obtain the latitude and longitude from the clicked point
            event.stopPropagation();
            var lat = event.mapPoint.latitude;
            var lng = event.mapPoint.longitude;
            console.log(lat, lng);
            // Call the Blazor method
            dotNetReference.invokeMethodAsync('AsyncMapOperation', lat, lng);
        });

        const measurement = new Measurement({
            view: view
        });

        view.ui.add(measurement, "top-right");

        const point = new Point({
            latitude: 34.751354,
            longitude: -92.274592
        });

        const markerSymbol = new SimpleMarkerSymbol({
            color: [226, 119, 40],
            outline: {
                color: [255, 255, 255],
                width: 1
            }
        });

        graphic = new Graphic({
            geometry: point,
            symbol: markerSymbol
        });

        graphicsLayer.add(graphic);

        view.center = point;

        view.on("context-menu", function (event) {
            event.stopPropagation(); // prevent default context menu from showing
            clearMeasurements();     // clear measurements and deactivate any active tools
        });

        const legend = new Legend({
            view: view,
            layerInfos: [{
                layer: featureLayer
            }]
        });

        //view.ui.add(legend, "bottom-right"); // you can change "bottom-right" to wherever you want to position the legend on the map


        const distanceButton = document.getElementById("distance");
        const areaButton = document.getElementById("area");
        const clearButton = document.getElementById("clear");
        //const switchButton = document.getElementById("switch-btn");

        distanceButton.addEventListener("click", distanceMeasurement);
        areaButton.addEventListener("click", areaMeasurement);
        clearButton.addEventListener("click", clearMeasurements);
        //switchButton.addEventListener("click", switchView);

        // Call the appropriate DistanceMeasurement2D or DirectLineMeasurement3D
        function distanceMeasurement() {
            const type = view.type;
            measurement.activeTool = type.toUpperCase() === "2D" ? "distance" : "direct-line";
            distanceButton.classList.add("active");
            areaButton.classList.remove("active");
        }

        // Call the appropriate AreaMeasurement2D or AreaMeasurement3D
        function areaMeasurement() {
            measurement.activeTool = "area";
            distanceButton.classList.remove("active");
            areaButton.classList.add("active");
        }

        // Clears all measurements
        function clearMeasurements() {
            distanceButton.classList.remove("active");
            areaButton.classList.remove("active");
            measurement.clear();
        }



    });
}

function updateArcMarkerPosition(lat, lng) {
    if (graphic && view) {
        const newPoint = { type: "point", latitude: lat, longitude: lng };
        graphic.geometry = newPoint;  // Update graphic's geometry
        console.log("Latitude:", lat, "Longitude:", lng);
        //view.center = newPoint;  // Update view's center: could add an autocenter here for users who want to disable it.
    }
}

//===================================================================================================================================
//===================================================================================================================================
//===================================================================================================================================




