// JM:
/**
 * Initial Setup, after DOM finished loading initializes important HTML elements like map, search controls, view toggles
 * 
 * Overview:
 * - Leaflet map initialization and location markers
 * - Fallback option (list-view) when map can't be used & option to switch between the two.
 * - Detecting current user location 
 * - Find recycling locations based on input or user location
 * - Validation
 * 
 */
document.addEventListener('DOMContentLoaded', function () {
    // JM:Location/Index page elements 
    const mapElement = document.getElementById('map'); //leaflet map instance
    const useCurrentCheckbox = document.getElementById('useCurrentLocation');
    const searchInput = document.getElementById('LocationSearch');
    const searchForm = document.getElementById('searchForm');
    const mapViewBtn = document.getElementById('mapViewBtn');
    const listViewBtn = document.getElementById('listViewBtn');
    const mapViewContainer = document.getElementById('mapViewContainer');
    const listViewContainer = document.getElementById('listViewContainer');

    // JM:Runtime state for map and location functionality
    let map = null;
    let mapUsable = false; 
    let userLocationMarker = null;
    let geoRequestId = 0;
    let tileErrorShown = false;

    // JM:
    /**
     * Display warning message above map/list 
     * Only when it is needed creates the notice/message
     * @param message Message displayed to user
     */
    function showMapNotice(message) {
        // JM:Instead of creating duplicates, existing ones are used
        let noticeEl = document.getElementById('mapNotice');

        if (!noticeEl) {
            noticeEl = document.createElement('div');
            noticeEl.id = 'mapNotice';
            noticeEl.className = 'alert alert-dark mb-2 d-none';
            noticeEl.setAttribute('role', 'alert');

            const anchor = mapViewContainer || mapElement;
            if (anchor?.parentNode) {
                anchor.parentNode.insertBefore(noticeEl, anchor);
            } else if (document.body) {
                document.body.prepend(noticeEl);
            } else {
                return;
            }
        }

        noticeEl.textContent = message;
        noticeEl.classList.remove('d-none');
    }
    
    //JM:
    /**
     * Convert coordinate value into number
     
     * @param value Coordinate value
     * @returns {number} Parsed coordinate or NaN
     */
    function parseCoordinate(value) {
        if (typeof value === 'number') {
            return value;
        }

        if (typeof value === 'string' && value.trim() !== '') {
            const parsed = Number(value);
            return Number.isFinite(parsed) ? parsed : NaN;
        }

        //JM: Convert Invalid, empty or non-numeric values allowing them to be rejected by isValidCoordinate() 
        return NaN;
    }

    //JM
    /**
     * Decides if latitude & longitude are valid world coordinates
     * @param lat lat Latitude between -90 and 90
     * @param lng lng Longitude between -180 and 180
     * @returns {boolean} True when both lat and lng are valid
     */
    function isValidCoordinate(lat, lng) {
        return Number.isFinite(lat) &&
            Number.isFinite(lng) &&
            lat >= -90 && lat <= 90 &&
            lng >= -180 && lng <= 180;
    }
    //JM
    /**
     * Location data stored in map element data attribute is parse
     * 
     * @param rawJson JSON-encoded location data
     * @returns {any|*[]} Parsed locations or an empty array
     */
    function parseLocations(rawJson) {
        try {
            const parsed = JSON.parse(rawJson || '[]');
            // JM: If not parsed disables map, list view is available
            if (!Array.isArray(parsed)) {
                showMapNotice('Map markers could not be loaded due to unexpected data format.');
                disableMapView();
                return [];
            }
            return parsed;
            // JM: Unexpected data disables map, list view is available
        } catch (err) {
            console.error('Invalid location JSON:', err);
            showMapNotice('Map markers could not be loaded, but available locations are listed in List View.');
            disableMapView();
            return [];
        }
    }

    //JM
    /**
     * Removes current marker from map
     */
    function removeUserMarker() {
        if (map && userLocationMarker) {
            map.removeLayer(userLocationMarker);
        }

        userLocationMarker = null;
    }

    //JM
    /**
     * Restore normal text search and clear current search
     * @param checkbox Current-location checkbox
     * @param latInput Latitude input
     * @param lngInput Longitude input
     * @param textInput Text search input
     * @param submitBtn Search button
     * @param statusEl Location status element
     */
    function resetLocationState(checkbox, latInput, lngInput, textInput, submitBtn, statusEl) {
        if (checkbox) checkbox.checked = false;
        if (latInput) latInput.value = '';
        if (lngInput) lngInput.value = '';
        if (textInput) textInput.disabled = false;
        if (submitBtn) submitBtn.disabled = false;
        if (statusEl) statusEl.textContent = '';
        removeUserMarker();
    }

    //JM
    /**
     * Update selected appearance of the toggle map/list buttons
     * Toggle purple button (active) and white button (not active)
     * @param button to update
     * @param isSelected if the button is active (clicked on by the user)
     */
    function updateToggleButton(button, isSelected) {
        if (!button) return;

        button.classList.toggle('btnpurple', isSelected);
        button.classList.toggle('text-white', isSelected);
        button.classList.toggle('active', isSelected);

        button.classList.toggle('bg-white', !isSelected);
        button.classList.toggle('text-dark', !isSelected);
        button.classList.toggle('border', !isSelected);

        button.setAttribute('aria-pressed', String(isSelected));
        button.setAttribute('aria-disabled', String(button.disabled));
    }

    //JM
    /**
     * Display map or list results
     * @param view requested button view (map or list) 
     */
    function setResultsView(view) {
        //JM: list view automatically used if map is unavailable
        const showMap = view === 'map' && map !== null && mapUsable;

        updateToggleButton(mapViewBtn, showMap);
        updateToggleButton(listViewBtn, !showMap);

        if (mapViewContainer) {
            mapViewContainer.classList.toggle('d-none', !showMap);
        }
        if (listViewContainer) {
            listViewContainer.classList.toggle('d-none', showMap);
        }

        if (showMap && map) {
            map.invalidateSize();
        }
    }

    //JM
    /**
     * Enable or disable browser geolocation (user location)
     * When enabled browser requests user location
     * @param checkbox Current-location checkbox to allow for geolocation
     */
    function toggleCurrentLocation(checkbox) {
        // JM: Retrieve form inputs, status display and submit button
        const latInput = document.getElementById('Latitude');
        const lngInput = document.getElementById('Longitude');
        const textInput = document.getElementById('LocationSearch');
        const statusEl = document.getElementById('locationStatus');
        const submitBtn = document.getElementById('SearchBtn');

        // JM: If any form inputs are missing then cancel
        if (!checkbox || !latInput || !lngInput || !textInput) return;

        //JM: Increment request ID to prevent previous geolocation callback from changing newer form state
        const currentRequestId = ++geoRequestId;

        //JM: If checkbox isn't check restore search and clear current search - restore text search
        if (!checkbox.checked) {
            resetLocationState(checkbox, latInput, lngInput, textInput, submitBtn, statusEl);
            return;
        }

        //JM: If geolocation isn't supported by the browser, inform the user. restore text search
        if (!navigator.geolocation) {
            resetLocationState(checkbox, latInput, lngInput, textInput, submitBtn, statusEl);
            alert('Geolocation is not supported by your browser.');
            return;
        }

        //JM: If geolocation is supported/checklist selected then disable text and button and inform user that their location is being detected
        textInput.value = '';
        textInput.disabled = true;
        if (submitBtn) submitBtn.disabled = true;
        if (statusEl) statusEl.textContent = 'Detecting your location...';

        
       
        try {
            // JM: request's current position
            navigator.geolocation.getCurrentPosition(
                //JM
                /**
                 * Success callback: runs when browser resolves the coordinates
                 * @param position coordinates
                 */
                function (position) {
                    
                    if (currentRequestId !== geoRequestId || !checkbox.checked) return;

                    // JM: Returns error if user has invalid position coordinates
                    if (!position || !position.coords) {
                        resetLocationState(
                            checkbox,
                            latInput,
                            lngInput,
                            textInput,
                            submitBtn,
                            statusEl
                        );

                        alert('Your browser returned an invalid location. Please enter a town or postcode.');
                        return;
                    }

                    
                    const lat = parseCoordinate(position.coords.latitude);
                    const lng = parseCoordinate(position.coords.longitude);

                    // JM: Returns error if user has invalid lat and lng coordinates
                    if (!isValidCoordinate(lat, lng)) {
                        resetLocationState(
                            checkbox,
                            latInput,
                            lngInput,
                            textInput,
                            submitBtn,
                            statusEl
                        );

                        alert('Your browser returned an invalid location. Please enter a town or postcode.');
                        return;
                    }

                    //JM: When successful hidden form fields are repopulated and search bar is enabled 
                    latInput.value = lat;
                    lngInput.value = lng;
                    if (submitBtn) submitBtn.disabled = false;
                    if (statusEl) statusEl.textContent = 'Current location ready.';

                    //JM: purple marker plot at user's coordinates, centers map
                    if (map && mapUsable && typeof L !== 'undefined') {
                        removeUserMarker();
                        userLocationMarker = L.circleMarker([lat, lng], { radius: 8, color: '#443445' })
                            .addTo(map)
                            .bindPopup('Your location');
                        map.setView([lat, lng], 13);
                    }
                },
                
                //JM
                /**
                 * Error callback: runs when permission denied, request times out, etc
                 * @param error displayed to user and in console
                 */
                function (error) {
                    if (currentRequestId !== geoRequestId || !checkbox.checked) return;
                    resetLocationState(checkbox, latInput, lngInput, textInput, submitBtn, statusEl);

                    // Display GeolocationPositionError error codes to user in an easier to understand format
                    const messages = {
                        1: 'Location permission was denied. Please allow location access in your browser.',
                        2: 'Your location could not be determined.',
                        3: 'Location request timed out. Please try again or enter a town or postcode.'
                    };
                    alert(messages[error && error.code] || 'Unable to retrieve your location.');
                },
                //JM: Cache location up to 60 seconds & stop waiting after 10
                { enableHighAccuracy: false, timeout: 10000, maximumAge: 60000 }
            );
            //JM: Browser errors & restore form
        } catch (err) {
            console.error('Geolocation getCurrentPosition error:', err);
            geoRequestId++;
            resetLocationState(checkbox, latInput, lngInput, textInput, submitBtn, statusEl);
            alert('Unable to start location detection. Please enter a town or postcode.');
        }
    }
    
    //JM: Switch results to display map
    if (mapViewBtn) {
        mapViewBtn.addEventListener('click', function () {
            setResultsView('map');
        });
    }
    //JM: Switch results to display list
    if (listViewBtn) {
        listViewBtn.addEventListener('click', function () {
            setResultsView('list');
        });
    }

    //JM: Trigger geolocation detection or resets current checkbox
    if (useCurrentCheckbox) {
        useCurrentCheckbox.addEventListener('change', function () {
            toggleCurrentLocation(useCurrentCheckbox);
        });
    }

    //JM: If user types into search, clears geolocation state
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const latInput = document.getElementById('Latitude');
            const lngInput = document.getElementById('Longitude');
            const statusEl = document.getElementById('locationStatus');
            const submitBtn = document.getElementById('SearchBtn');

            const hasLocationState =
                (useCurrentCheckbox && useCurrentCheckbox.checked) ||
                (latInput && latInput.value) ||
                (lngInput && lngInput.value);

            if (hasLocationState) {
                geoRequestId++;
                resetLocationState(
                    useCurrentCheckbox,
                    latInput,
                    lngInput,
                    searchInput,
                    submitBtn,
                    statusEl
                );
            }
        });
    }


    //JM: validate coordinate readiness or clear stale coordinates before POST
    if (searchForm) {
        searchForm.addEventListener('submit', function (event) {
            const latInput = document.getElementById('Latitude');
            const lngInput = document.getElementById('Longitude');

            const value = searchInput?.value.trim().replace(/\s+/g, ' ') || '';

            const hasValidCurrentLocation =
                useCurrentCheckbox?.checked &&
                isValidCoordinate(
                    parseCoordinate(latInput?.value),
                    parseCoordinate(lngInput?.value)
                );

            if (!value && !hasValidCurrentLocation) {
                event.preventDefault();
                showMapNotice('Please enter a town or postcode, or use your current location.');
                searchInput?.focus();
                return;
            }

            if (hasValidCurrentLocation) {
                return;
            }

            const normalizedPostcode = value.toUpperCase();

            const validTown =
                /^[A-Za-zÀ-ÿ]+(?:[ .'\u2019-][A-Za-zÀ-ÿ]+)*$/.test(value) &&
                (value.match(/[A-Za-zÀ-ÿ]/g) || []).length >= 2;

            const validPostcode =
                /^(?:GIR 0AA|[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2})$/.test(normalizedPostcode);

            const validInput = validTown || validPostcode;

            if (!validInput) {
                event.preventDefault();
                showMapNotice('Please enter a valid town or postcode.');
                searchInput.focus();
                return;
            }
            
            if (useCurrentCheckbox?.checked) {
                const lat = parseCoordinate(latInput?.value);
                const lng = parseCoordinate(lngInput?.value);

                if (!isValidCoordinate(lat, lng)) {
                    event.preventDefault();

                    const message = 'Your location is not ready. Uncheck “Use current location” or try detecting it again.';
                    const statusEl = document.getElementById('locationStatus');

                    if (statusEl) {
                        statusEl.textContent = message;
                        statusEl.hidden = false;
                    }

                    showMapNotice(message);
                    return;
                }
            } else {
                if (latInput) latInput.value = '';
                if (lngInput) lngInput.value = '';
            }
        });
        searchForm.addEventListener('invalid', function (event) {
            event.preventDefault();
            showMapNotice('Please enter a valid town or postcode.');
        }, true);
    }
    
    //JM: 
    /**
     * Disable map view and select list for fallback
     * Used when Leaflet is missing & map cannot start, data is invalid, map tiles fail repeatly
     */
    function disableMapView() {
        mapUsable = false; //JM: map mark as unavailable
        
        if (mapViewBtn) {
            // JM: Prevent user selecting broken map
            mapViewBtn.disabled = true;
            mapViewBtn.setAttribute('aria-disabled', 'true');
        }
        //JM: List is displayed
        setResultsView('list');
    }

    // JM: Leaflet loaded as optional, and if it is unavailable the list is usable.
    if (typeof L === 'undefined') {
        if (mapViewBtn) {
            mapViewBtn.disabled = true;
        }
        showMapNotice('Interactive map is currently unavailable. Switched to list view.');
        disableMapView();
        
        // JM: If map element missing from HTML, use list instead
    } else if (!mapElement) {
        showMapNotice('Map container is unavailable. Switched to list view.');
        disableMapView();
        

        // JM: When Leaflet & map container are available,initialise leaflet map 
    } else {
        try {
            // JM: Creates map, default coordinates point to Glasgow
            map = L.map(mapElement).setView([55.8642, -4.2518], 12);
            mapUsable = true;
            
            // JM: OpenStreetMap tiles as background
            const tileLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; OpenStreetMap contributors'
            });


            /*
            * JM: Count tile loading errors.
            * Map disabled after 3 repeated failures
             */
            let tileErrorCount = 0;
            const maxTileErrors = 3;

            tileLayer.on('tileerror', function () {
                tileErrorCount++;

                //JM: If maxium num of failures/errors reached 3, then display error to user and disable map view
                if (tileErrorCount < maxTileErrors || tileErrorShown) {
                    return;
                }
                tileErrorShown = true;
                showMapNotice(
                    'Map background tiles failed to load. You can still view pins in List View.'
                );
                disableMapView();
            });

            //JM: Tile layer added to map
            tileLayer.addTo(map);

            
            const locations = parseLocations(mapElement.dataset.locations);

            // JM: Create marker for every location in locations array - multiple map markers
            if (locations.length > 0) {
                //JM: Track total area containing valid marker for map to show all markers together
                const bounds = new L.LatLngBounds();
                //JM: Locations that can't be plotted
                let skippedCount = 0;

                locations.forEach(function (loc, index) {
                    //JM: Ignore values that aren't location objects
                    if (!loc || typeof loc !== 'object') {
                        skippedCount++;
                        return;
                    }

                    const lat = parseCoordinate(loc.lat);
                    const lng = parseCoordinate(loc.lng);

                    //JM: Skip lcations with mising/invalid coordinates 
                    if (!isValidCoordinate(lat, lng)) {
                        skippedCount++;
                        return;
                    }

                    
                    const listPosition = index + 1;

                    //JM: popup content and title
                    const popupContent = document.createElement('div');
                    const titleEl = document.createElement('strong');

                    //JM: Use name of recycling location, if not possible use default marker title 'Recycling location'
                    const locationName = (typeof loc.name === 'string' && loc.name.trim())
                        ? loc.name.trim()
                        : 'Recycling Location';

                    //JM: Add location title and position num to popup
                    titleEl.textContent = `#${listPosition} ${locationName}`;
                    popupContent.appendChild(titleEl);

                    //JM: Add address when available
                    if (typeof loc.address === 'string' && loc.address.trim()) {
                        popupContent.appendChild(document.createElement('br'));
                        const addressEl = document.createElement('span');
                        addressEl.textContent = loc.address;
                        popupContent.appendChild(addressEl);
                    }

                    //JM: Create & add one marker for the location, repeat for each location in the array
                    L.marker([lat, lng]).addTo(map).bindPopup(popupContent);
                    bounds.extend([lat, lng]);
                });

                //JM: Adjust map to make all valid marker visible
                if (bounds.isValid()) {
                    map.fitBounds(bounds, {padding: [30, 30]});
                }

                //JM: Display error message when there's issue with plotting locations
                if (skippedCount > 0) {
                    showMapNotice(`${skippedCount} location(s) could not be plotted on the map due to missing or invalid coordinates.`);
                }
            }
            //JM: Catch errors thrown during map creation, tile setup, location parsing, marker creation
        } catch (err) {
            console.error('Leaflet map initialization failed:', err);

            if (map) {
                map.remove();
            }
            
            //JM: Reset map state
            map = null;
            mapUsable = false;

            // Disable view map button
            if (mapViewBtn) {
                mapViewBtn.disabled = true;
            }
            
            showMapNotice('Interactive map could not be initialized. Switched to list view.');
            setResultsView('list');
        }
    }
    
    //JM: Restore location state when page loads with checkbox selected (page refresh/form submision)
    if (useCurrentCheckbox && useCurrentCheckbox.checked) {
        const latInput = document.getElementById('Latitude');
        const lngInput = document.getElementById('Longitude');
        const statusEl = document.getElementById('locationStatus');
        const submitBtn = document.getElementById('SearchBtn');

        const lat = parseCoordinate(latInput ? latInput.value : null);
        const lng = parseCoordinate(lngInput ? lngInput.value : null);

        if (isValidCoordinate(lat, lng) && searchInput) {
            searchInput.disabled = true;
            if (submitBtn) submitBtn.disabled = false;
            if (statusEl) statusEl.textContent = 'Current location ready.';

            if (map && mapUsable && typeof L !== 'undefined') {
                userLocationMarker = L.circleMarker([lat, lng], {
                    radius: 8,
                    color: '#443445'
                }).addTo(map).bindPopup('Your location');
            }
        } else {
            resetLocationState(
                useCurrentCheckbox,
                latInput,
                lngInput,
                searchInput,
                submitBtn,
                statusEl
            );
        }
    }
});