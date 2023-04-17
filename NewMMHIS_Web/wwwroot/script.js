
window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    };
};

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

function callUpdateMap2(latitude, longitude) {
    DotNet.invokeMethodAsync('NewMMHIS_Web', 'UpdateMap2', latitude, longitude);
}

function setMap() {
    require([
        "esri/layers/GraphicsLayer",
        "esri/Map",
        "esri/views/MapView",
        "esri/views/SceneView",
        //"esri/layers/TileLayer",
        "esri/layers/FeatureLayer",
        "esri/widgets/Measurement",
        "esri/widgets/Search", //need to comment this out in order for the legend to work?
        "esri/geometry/Point",
        "esri/symbols/SimpleMarkerSymbol",
        "esri/widgets/Legend",
        "esri/Graphic"
    ], (
        GraphicsLayer,
        Map,
        MapView,
        SceneView,
        //TileLayer,
        FeatureLayer,
        Measurement,
        Search,
        Point,
        SimpleMarkerSymbol,
        Legend,
        Graphic
    ) => {
        const graphicsLayer = new GraphicsLayer({
            elevationInfo: {
                mode: "relative-to-ground",
                offset: 1 // You can adjust this value to raise the marker above the ground if needed
            }
        });

        const initialFeature = "https://gis.ardot.gov/hosting/rest/services/SIR_TIS/Road_Inventory_OnSystem/FeatureServer/0";

        const featureLayer = new FeatureLayer({
            url: initialFeature
        });

        const featureLayerSelect = document.getElementById("feature-layer-select");
        featureLayerSelect.addEventListener("change", (event) => {
            const newFeatureLayerUrl = event.target.value;
            updateFeatureLayer(newFeatureLayerUrl);
        });

        function updateFeatureLayer(newFeatureLayerUrl) {
            const newFeatureLayer = new FeatureLayer({
                url: newFeatureLayerUrl
            });

            // Remove the existing feature layer from the map
            map.remove(featureLayer);

            // Add the new feature layer to the map
            map.add(newFeatureLayer, 0); // Adjust the index as needed based on your layer order

            // Update the legend widget
            legend.layerInfos = [{
                layer: newFeatureLayer,
                title: "Legend"
            }];

            // Update the featureLayer variable
            featureLayer = newFeatureLayer;
        }

        const initialBasemap = "streets-navigation-vector";

        // Create new Map with TileLayer and FeatureLayer
        const map = new Map({
            basemap: initialBasemap,
            layers: [featureLayer, graphicsLayer]
            //layers: [graphicsLayer]
        });

        const basemapSelect = document.getElementById("basemap-select");
        basemapSelect.addEventListener("change", (event) => {
            map.basemap = event.target.value;
        });



        // Create MapView with defined zoom and center
        const mapView = new MapView({
            zoom: 15,
            center: [-92.274592, 34.751354],
            map: map
        });

        // Create SceneView with similar extent to MapView
        const sceneView = new SceneView({
            scale: 123456789,
            center: [-92.274592, 34.751354],
            map: map
        });

        // Set the activeView to the 2D MapView
        let activeView = mapView;

        // Create new instance of the Measurement widget
        const measurement = new Measurement();

        //Create new instance of the Legend widget
        const legend = new Legend({
            layerInfos: [{
                layer: featureLayer,
                title: "Legend"
            }]
        });

        const searchWidget = new Search({
            view: activeView
        });
        // Adds the search widget below other elements in
        // the top left corner of the view
        activeView.ui.add(searchWidget, {
            position: "top-right",
            index: 2
        });

        // Set-up event handlers for buttons and click events
        const switchButton = document.getElementById("switch-btn");
        const distanceButton = document.getElementById('distance');
        const areaButton = document.getElementById('area');
        const clearButton = document.getElementById('clear');

        switchButton.addEventListener("click", () => {
            switchView();
        });
        distanceButton.addEventListener("click", () => {
            distanceMeasurement();
        });
        areaButton.addEventListener("click", () => {
            areaMeasurement();
        });
        clearButton.addEventListener("click", () => {
            clearMeasurements();
        });

        // Call the loadView() function for the initial view
        loadView();

        // The loadView() function to define the view for the widgets and div
        function loadView() {
            activeView.set({
                container: "viewDiv"
            });
            // Add the appropriate measurement UI to the bottom-right when activated
            activeView.ui.add(measurement, "bottom-right");
            // Add the legend to the bottom left
            activeView.ui.add(legend, "bottom-left");
            // Set the views for the widgets
            measurement.view = activeView;
            legend.view = activeView;
        }

        // When the 2D or 3D button is activated, the switchView() function is called
        function switchView() {
            // Clone the viewpoint for the MapView or SceneView
            const viewpoint = activeView.viewpoint.clone();
            // Get the view type, either 2d or 3d
            const type = activeView.type;

            // Clear any measurements that had been drawn
            clearMeasurements();

            // Reset the measurement tools in the div
            activeView.container = null;
            activeView = null;
            // Set the view based on whether it switched to 2D MapView or 3D SceneView
            activeView = type.toUpperCase() === "2D" ? sceneView : mapView;
            activeView.set({
                container: "viewDiv",
                viewpoint: viewpoint
            });
            // Add the appropriate measurement UI to the bottom-right when activated
            activeView.ui.add(measurement, "bottom-right");
            // Add the legend to the bottom left
            activeView.ui.add(legend, "bottom-left");

            // Set the views for the widgets
            measurement.view = activeView;
            legend.view = activeView;
            // Reset the value of the 2D or 3D switching button
            switchButton.value = type.toUpperCase();
        }

        // Call the appropriate DistanceMeasurement2D or DirectLineMeasurement3D
        function distanceMeasurement() {
            const type = activeView.type;
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

        //====================================================================
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

        const graphic = new Graphic({
            geometry: point,
            symbol: markerSymbol
        });

        graphicsLayer.add(graphic);

        activeView.center = point;
        window.updateMap = (latitude, longitude) => {
            
            // Create a new Point object
            var point = {
                type: "point",
                longitude: longitude,
                latitude: latitude,
            };

            // Create a new Graphic object with a simple marker symbol
            var graphic = {
                geometry: point,
                symbol: {
                    type: "simple-marker",
                    color: [226, 119, 40],
                    outline: {
                        color: [255, 255, 255],
                        width: 1,
                    },
                },
            };

            // Add the new graphic to the GraphicsLayer
            graphicsLayer.removeAll();
            graphicsLayer.add(graphic);
            activeView.center = point;
        }


        // Listen for the click event on the MapView
        activeView.on("click", function (event) {
            
            // Get the coordinates of the click
            var lat = event.mapPoint.latitude;
            var lon = event.mapPoint.longitude;

            // Do something with the coordinates
            console.log("Lat: " + lat + ", Lon: " + lon);
            scrollToTop(1000);
            callUpdateMap2(lat, lon);
        });

    });
}

window.initializeMap = () => {
    setMap();
}

function scrollToTop(duration) {
    const startPosition = document.documentElement.scrollTop || document.body.scrollTop;
    const startTime = performance.now();

    function scroll(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);

        const easeInOutCubic = (t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        const currentPosition = startPosition + (0 - startPosition) * easeInOutCubic(progress);

        window.scrollTo(0, currentPosition);

        if (progress < 1) {
            requestAnimationFrame(scroll);
        }
    }

    requestAnimationFrame(scroll);
}


