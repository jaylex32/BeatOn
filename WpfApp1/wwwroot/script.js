function updateStatus() {
    fetch('/api/status')
        .then(response => response.json())
        .then(data => {
            document.getElementById('status').textContent = `Status: ${data.Status}`;
            document.getElementById('downloadCount').textContent = `Total Downloads: ${data.DownloadCount}`;
            
            const recentDownloads = document.getElementById('recentDownloads');
            recentDownloads.innerHTML = '';
            data.RecentDownloads.forEach(download => {
                const li = document.createElement('li');
                li.textContent = `${download.ArtistName} - ${download.AlbumName}`;
                recentDownloads.appendChild(li);
            });
        })
        .catch(error => console.error('Error:', error));
}

// Update status every 5 seconds
setInterval(updateStatus, 5000);

// Initial update
updateStatus();