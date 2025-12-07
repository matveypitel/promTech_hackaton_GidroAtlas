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

            if (condition && condition >= 1 && condition <= 5) {
                // Colored CircleMarker for Admin/Conditions
                let color = '#808080';
                switch (condition) {
                    case 1: color = '#4CAF50'; break;
                    case 2: color = '#AED581'; break;
                    case 3: color = '#FFEB3B'; break;
                    case 4: color = '#FF9800'; break;
                    case 5: color = '#F44336'; break;
                }

                const marker = L.circleMarker([lat, lng], {
                    radius: 10,
                    fillColor: color,
                    color: '#fff',
                    weight: 2,
                    opacity: 1,
                    fillOpacity: 0.9
                }).addTo(this.map);

                this.markers[markerId] = marker;

                marker.bindTooltip(`<b>${title}</b><br/>${region}`, {
                    direction: 'top',
                    offset: [0, -10]
                });
            } else {
                // Standard Marker for Guest
                const marker = L.marker([lat, lng]).addTo(this.map);
                this.markers[markerId] = marker;
                marker.bindPopup(`<b>${title}</b><br/>${region}`);
            }

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
    },

    removeMarker: function (markerId) {
        try {
            const marker = this.markers[markerId];
            if (marker) {
                this.map.removeLayer(marker);
                delete this.markers[markerId];
                console.log(`Marker removed: ${markerId}`);
            }
        } catch (error) {
            console.error('Error removing marker:', error);
        }
    },

    clearMarkers: function () {
        try {
            for (const markerId in this.markers) {
                if (this.markers.hasOwnProperty(markerId)) {
                    this.map.removeLayer(this.markers[markerId]);
                }
            }
            this.markers = {};
            console.log('All markers cleared');
        } catch (error) {
            console.error('Error clearing markers:', error);
        }
    }
};
