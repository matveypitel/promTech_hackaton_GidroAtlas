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

    addMarker: function (lat, lng, title, markerId) {
        try {
            if (!this.map) {
                console.error('Map not initialized');
                return;
            }

            // Создаем маркер
            const marker = L.marker([lat, lng]).addTo(this.map);

            // Сохраняем маркер
            this.markers[markerId] = marker;

            // Добавляем всплывающую подсказку
            marker.bindPopup(`<b>${title}</b>`);

            console.log(`Marker added: ${title} at [${lat}, ${lng}]`);
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
