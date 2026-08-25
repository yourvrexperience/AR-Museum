var OpenWindowPlugin = {
    OpenInNewTab: function(link)
    {
		// window.open(Pointer_stringify(link));
		window.open(UTF8ToString(link));
    },
	OpenInSameTab: function (link) {
		// window.location = Pointer_stringify(link);
		window.location = UTF8ToString(link);
		// or window.location.replace(Pointer_stringify(link));
	},
	DownloadFile: function (filename, fileData, dataLength) {
        // Convert the filename pointer to a JS string
        // var name = Pointer_stringify(filename);
		var name = UTF8ToString(filename);

        // Create a typed array from the file bytes in WASM memory
        var bytes = new Uint8Array(dataLength);
        for (var i = 0; i < dataLength; i++) {
            bytes[i] = HEAPU8[fileData + i];
        }

        // Create a blob of type image/png; adjust if using other formats
        var blob = new Blob([bytes], { type: "image/png" });

        // Create a temporary link element
        var url = window.URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = name;
        document.body.appendChild(link);

        // Trigger the download
        link.click();

        // Cleanup
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }	
};

mergeInto(LibraryManager.library, OpenWindowPlugin);