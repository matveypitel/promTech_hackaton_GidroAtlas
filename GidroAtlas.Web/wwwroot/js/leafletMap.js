window.leafletMap = {
    map: null,
    markers: {},

    create: function (mapId, lat, lng, zoom) {
        try {
            // Создаем карту
            this.map = L.map(mapId).setView([lat, lng], zoom);

            // Добавляем тайлы OpenStreetMap
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(this.map);

            console.log('Map created successfully');
        } catch (error) {
            console.error('Error creating map:', error);
        }
    },

    addMarker: function (lat, lng, title, markerId, condition, region) {
        try {
            if (!this.map) {
                console.error('Map not initialized');
                return;
            }

            // Determine color based on technical condition (1-5)
            let color = '#808080'; // Default gray
            switch (condition) {
                case 1: color = '#4CAF50'; break; // Green
                case 2: color = '#AED581'; break; // Salad/Light Green
                case 3: color = '#FFEB3B'; break; // Yellow
                case 4: color = '#FF9800'; break; // Orange
                case 5: color = '#F44336'; break; // Red
            }

            // Create CircleMarker
            const marker = L.circleMarker([lat, lng], {
                radius: 10,
                fillColor: color,
                color: '#fff', // Border color
                weight: 2,
                opacity: 1,
                fillOpacity: 0.9
            }).addTo(this.map);

            // Save marker
            this.markers[markerId] = marker;

            // Add tooltip for hover (Name and Region)
            marker.bindTooltip(`<b>${title}</b><br/>${region}`, {
                direction: 'top',
                offset: [0, -10]
            });

            console.log(`Marker added: ${title} [Cond:${condition}] at [${lat}, ${lng}]`);
        } catch (error) {
            console.error('Error adding marker:', error);
        }
    },

    setMarkerClickCallback: function (dotnetHelper, markerId) {
        try {
            const marker = this.markers[markerId];
            if (marker) {
                marker.on('click', function () {
                    dotnetHelper.invokeMethodAsync('OnMarkerClicked', markerId);
                });
            }
        } catch (error) {
            console.error('Error setting marker click callback:', error);
        }
    }
};
