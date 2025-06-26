document.addEventListener('DOMContentLoaded', function() {
    const provinceSelect = document.getElementById('province-filter');
    const districtSelect = document.getElementById('district-filter');
    
    // Dictionary of districts for each province
    const districtsByProvince = {
        '1': [ // Hồ Chí Minh
            { id: '1', name: 'Quận 1' },
            { id: '2', name: 'Quận 2' },
            { id: '3', name: 'Quận 3' },
            { id: '4', name: 'Quận 4' },
            { id: '5', name: 'Quận 5' },
            { id: '6', name: 'Quận 6' },
            { id: '7', name: 'Quận 7' },
            { id: '8', name: 'Quận 8' },
            { id: '9', name: 'Quận 9' },
            { id: '10', name: 'Quận 10' },
            { id: '11', name: 'Quận 11' },
            { id: '12', name: 'Quận 12' },
            { id: '13', name: 'Quận Thủ Đức' },
            { id: '14', name: 'Quận Gò Vấp' },
            { id: '15', name: 'Quận Bình Thạnh' },
            { id: '16', name: 'Quận Tân Bình' },
            { id: '17', name: 'Quận Tân Phú' },
            { id: '18', name: 'Quận Phú Nhuận' },
            { id: '19', name: 'Quận Bình Tân' },
            { id: '20', name: 'Huyện Củ Chi' },
            { id: '21', name: 'Huyện Hóc Môn' },
            { id: '22', name: 'Huyện Bình Chánh' },
            { id: '23', name: 'Huyện Nhà Bè' },
            { id: '24', name: 'Huyện Cần Giờ' }
        ],
        '2': [ // Hà Nội
            { id: '1', name: 'Quận Ba Đình' },
            { id: '2', name: 'Quận Hoàn Kiếm' },
            { id: '3', name: 'Quận Tây Hồ' },
            { id: '4', name: 'Quận Long Biên' },
            { id: '5', name: 'Quận Cầu Giấy' },
            { id: '6', name: 'Quận Đống Đa' },
            { id: '7', name: 'Quận Hai Bà Trưng' },
            { id: '8', name: 'Quận Hoàng Mai' },
            { id: '9', name: 'Quận Thanh Xuân' },
            { id: '10', name: 'Huyện Sóc Sơn' },
            { id: '11', name: 'Huyện Đông Anh' },
            { id: '12', name: 'Huyện Gia Lâm' },
            { id: '13', name: 'Quận Nam Từ Liêm' },
            { id: '14', name: 'Huyện Thanh Trì' },
            { id: '15', name: 'Quận Hà Đông' },
            { id: '16', name: 'Thị xã Sơn Tây' },
            { id: '17', name: 'Huyện Ba Vì' },
            { id: '18', name: 'Huyện Phúc Thọ' },
            { id: '19', name: 'Huyện Thạch Thất' },
            { id: '20', name: 'Huyện Quốc Oai' },
            { id: '21', name: 'Huyện Chương Mỹ' },
            { id: '22', name: 'Huyện Đan Phượng' },
            { id: '23', name: 'Huyện Hoài Đức' },
            { id: '24', name: 'Huyện Thường Tín' },
            { id: '25', name: 'Huyện Phú Xuyên' },
            { id: '26', name: 'Huyện Mỹ Đức' },
            { id: '27', name: 'Huyện Ứng Hòa' },
            { id: '28', name: 'Huyện Mê Linh' }
        ]
        // Add more provinces and their districts here
    };

    // Function to update district options
    function updateDistricts(provinceId) {
        // Clear current options
        districtSelect.innerHTML = '<option value="">Chọn quận huyện</option>';
        
        // If a province is selected, add its districts
        if (provinceId && districtsByProvince[provinceId]) {
            districtsByProvince[provinceId].forEach(district => {
                const option = document.createElement('option');
                option.value = district.id;
                option.textContent = district.name;
                districtSelect.appendChild(option);
            });
        }
    }

    // Event listener for province selection
    provinceSelect.addEventListener('change', function() {
        updateDistricts(this.value);
    });

    // Event listener for search button
    document.getElementById('locator-submit').addEventListener('click', function() {
        const selectedProvince = provinceSelect.value;
        const selectedDistrict = districtSelect.value;
        
        if (!selectedProvince) {
            alert('Vui lòng chọn tỉnh/thành phố');
            return;
        }
        
        if (!selectedDistrict) {
            alert('Vui lòng chọn quận/huyện');
            return;
        }

        // Here you can add the logic to search for stores
        console.log('Searching for stores in:', {
            province: provinceSelect.options[provinceSelect.selectedIndex].text,
            district: districtSelect.options[districtSelect.selectedIndex].text
        });
    });
}); 