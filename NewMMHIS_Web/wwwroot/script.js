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

async function loadImage(imageUrl) {
    let img;
    const imageLoadPromise = new Promise(resolve => {
        img = new Image();
        img.onload = resolve;
        img.src = imageUrl;
    });

    await imageLoadPromise;
    console.log("image loaded");
    return img;
}

function preloadImages(array) {
    if (!preloadImages.list) {
        preloadImages.list = [];
    }
    var list = preloadImages.list;
    for (var i = 0; i < array.length; i++) {
        var img = new Image();
        img.onload = function () {
            var index = list.indexOf(this);
            if (index !== -1) {
                // remove image from the array once it's loaded
                // for memory consumption reasons
                list.splice(index, 1);
            }
        }
        list.push(img);
        img.src = array[i];
    }
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
// Initialize variables globally
//var map, view, graphicsLayer, Point;

//var initialized = false;

//function setMap() {
//    require([
//        "esri/config",
//        "esri/Map",
//        "esri/views/MapView",
//        "esri/widgets/Measurement",
//        "esri/layers/GraphicsLayer",
//        "esri/geometry/Point",
//        "esri/symbols/SimpleMarkerSymbol",
//        "esri/Graphic"
//    ], function (esriConfig, Map, MapView, Measurement, GraphicsLayer, Point, SimpleMarkerSymbol, Graphic) {
//        esriConfig.apiKey = "";

//        const graphicsLayer = new GraphicsLayer();

//        const map = new Map({
//            basemap: "streets-navigation-vector",
//            layers: [graphicsLayer]
//        });

//        const view = new MapView({
//            container: "viewDiv",
//            map: map,
//            zoom: 15
//        });

//        const measurement = new Measurement({
//            view: view,
//            activeTool: "distance"
//        });

//        view.ui.add(measurement, "top-right");

//        const point = new Point({
//            latitude: 34.751354,
//            longitude: -92.274592
//        });

//        const markerSymbol = new SimpleMarkerSymbol({
//            color: [226, 119, 40],
//            outline: {
//                color: [255, 255, 255],
//                width: 1
//            }
//        });

//        const graphic = new Graphic({
//            geometry: point,
//            symbol: markerSymbol
//        });

//        graphicsLayer.add(graphic);

//        view.center = point;

//        //just added
//        // Define the updateMap function as a closure with access to the GraphicsLayer and MapView objects
//        window.updateMap = (latitude, longitude) => {
//            // Create a new Point object
//            var point = {
//                type: "point",
//                longitude: longitude,
//                latitude: latitude,
//            };

//            // Create a new Graphic object with a simple marker symbol
//            var graphic = {
//                geometry: point,
//                symbol: {
//                    type: "simple-marker",
//                    color: [226, 119, 40],
//                    outline: {
//                        color: [255, 255, 255],
//                        width: 1,
//                    },
//                },
//            };

//            // Add the new graphic to the GraphicsLayer
//            graphicsLayer.removeAll();
//            graphicsLayer.add(graphic);
//            view.center = point;
//        }
        

//        // Switch between area and distance measurement
//        function switchTool() {
//            const tool = measurement.activeTool === "distance" ? "area" : "distance";
//            measurement.activeTool = tool;
//        }
//    });
//}


//function updateMap(latitudeId, longitudeId, map) {
//    require([
//        "esri/Graphic",
//        "esri/layers/GraphicsLayer",
//        "esri/geometry/Point"
//    ], function (Graphic, GraphicsLayer, Point) {
//        // Get the graphics layer and view from the map
//        var graphicsLayer = map.findLayerById("graphicsLayer");
//        var view = mapView;

//        // Get the latitude and longitude values from the C# code
//        var latitude = document.getElementById(latitudeId).value;
//        var longitude = document.getElementById(longitudeId).value;

//        // Create a new point using the latitude and longitude values
//        var point = new Point({
//            latitude: latitude,
//            longitude: longitude
//        });

//        // Create a new graphic using the point and add it to the graphics layer
//        var graphic = new Graphic({
//            geometry: point,
//            symbol: {
//                type: "simple-marker",
//                color: "red",
//                size: "12px",
//                outline: {
//                    color: [255, 255, 255],
//                    width: 1
//                }
//            }
//        });

//        graphicsLayer.add(graphic);
//    });
//}




