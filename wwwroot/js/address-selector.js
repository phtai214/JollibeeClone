// Address Selector JavaScript
// Handles cascading dropdown for City > District > Ward selection

function initializeAddressSelectors(currentCity = '', currentDistrict = '', currentWard = '') {
    // Load provinces on initialization
    loadProvinces(currentCity, currentDistrict, currentWard);

    // Address cascade functionality
    $('#citySelect').on('change', function() {
        const provinceCode = $(this).val();
        $('#districtSelect').empty().append('<option value="">Chọn quận/huyện...</option>').prop('disabled', true);
        $('#wardSelect').empty().append('<option value="">Chọn phường/xã...</option>').prop('disabled', true);
        
        // Update hidden field
        const cityText = $('#citySelect option:selected').text();
        if (cityText && cityText !== 'Chọn tỉnh/thành phố...' && cityText !== 'Đang tải...') {
            $('#City').val(cityText);
        } else {
            $('#City').val('');
        }
        
        if (provinceCode) {
            loadDistricts(provinceCode);
        }
    });

    $('#districtSelect').on('change', function() {
        const districtCode = $(this).val();
        $('#wardSelect').empty().append('<option value="">Chọn phường/xã...</option>').prop('disabled', true);
        
        // Update hidden field
        const districtText = $('#districtSelect option:selected').text();
        if (districtText && districtText !== 'Chọn quận/huyện...' && districtText !== 'Đang tải...') {
            $('#District').val(districtText);
        } else {
            $('#District').val('');
        }
        
        if (districtCode) {
            loadWards(districtCode);
        }
    });

    $('#wardSelect').on('change', function() {
        const wardValue = $(this).val();
        $('#Ward').val(wardValue);
    });
}

function loadProvinces(currentCity = '', currentDistrict = '', currentWard = '') {
    $('#citySelect').html('<option value="">Đang tải...</option>').prop('disabled', true);
    
    $.ajax({
        url: 'https://provinces.open-api.vn/api/',
        method: 'GET',
        success: function(data) {
            $('#citySelect').empty().append('<option value="">Chọn tỉnh/thành phố...</option>').prop('disabled', false);
            
            let selectedProvinceCode = null;
            data.forEach(function(province) {
                const isSelected = province.name === currentCity;
                $('#citySelect').append(`<option value="${province.code}" data-name="${province.name}" ${isSelected ? 'selected' : ''}>${province.name}</option>`);
                if (isSelected) {
                    selectedProvinceCode = province.code;
                }
            });

            // Load districts if city is selected
            if (selectedProvinceCode) {
                loadDistricts(selectedProvinceCode, currentDistrict, currentWard);
            }
        },
        error: function() {
            $('#citySelect').html('<option value="">Lỗi tải dữ liệu</option>');
            console.error('Không thể tải danh sách tỉnh thành');
        }
    });
}

function loadDistricts(provinceCode, currentDistrict = '', currentWard = '') {
    $('#districtSelect').html('<option value="">Đang tải...</option>').prop('disabled', true);
    
    $.ajax({
        url: `https://provinces.open-api.vn/api/p/${provinceCode}?depth=2`,
        method: 'GET',
        success: function(data) {
            $('#districtSelect').empty().append('<option value="">Chọn quận/huyện...</option>').prop('disabled', false);
            
            let selectedDistrictCode = null;
            if (data.districts) {
                data.districts.forEach(function(district) {
                    const isSelected = district.name === currentDistrict;
                    $('#districtSelect').append(`<option value="${district.code}" data-name="${district.name}" ${isSelected ? 'selected' : ''}>${district.name}</option>`);
                    if (isSelected) {
                        selectedDistrictCode = district.code;
                    }
                });
            }

            // Load wards if district is selected
            if (selectedDistrictCode) {
                loadWards(selectedDistrictCode, currentWard);
            }
        },
        error: function() {
            $('#districtSelect').html('<option value="">Lỗi tải dữ liệu</option>');
            console.error('Không thể tải danh sách quận huyện');
        }
    });
}

function loadWards(districtCode, currentWard = '') {
    $('#wardSelect').html('<option value="">Đang tải...</option>').prop('disabled', true);
    
    $.ajax({
        url: `https://provinces.open-api.vn/api/d/${districtCode}?depth=2`,
        method: 'GET',
        success: function(data) {
            $('#wardSelect').empty().append('<option value="">Chọn phường/xã...</option>').prop('disabled', false);
            
            if (data.wards) {
                data.wards.forEach(function(ward) {
                    const isSelected = ward.name === currentWard;
                    $('#wardSelect').append(`<option value="${ward.name}" ${isSelected ? 'selected' : ''}>${ward.name}</option>`);
                });
            }
        },
        error: function() {
            $('#wardSelect').html('<option value="">Lỗi tải dữ liệu</option>');
            console.error('Không thể tải danh sách phường xã');
        }
    });
} 