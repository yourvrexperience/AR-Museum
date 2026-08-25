mergeInto(LibraryManager.library, 
{
	WebFileBrowserDownload: function(fileName, base64Data)
    {
		var element = document.createElement('a');
        
        element.setAttribute('href', 'data:application/octet-stream;base64,' + encodeURIComponent(UTF8ToString(base64Data)));
        element.setAttribute('download', UTF8ToString(fileName));
        element.style.display = 'none';
        document.body.appendChild(element);
        element.click();
        document.body.removeChild(element);
    },
    WebFileBrowserUpload: function(extensionFilter, receiverName, methodName)
    {
        if (typeof inputLoader == "undefined")
        {
            inputLoader = document.createElement("input");
            inputLoader.setAttribute("type", "file");
			inputLoader.style.display = 'none';
            
            document.body.appendChild(inputLoader);

            inputLoader.onchange = 
                function(x)
                {
                    if(this.value == "") return;

                    var file = this.files[0];
					var reader = new FileReader();
                    this.value = "";
                    var thisInput = this;
                    
                    reader.onload = function(evt) 
                    {
	                    if (evt.target.readyState != 2)
		                    return;

	                    if (evt.target.error) 
	                    {
		                    alert("Error while reading file " + file.name + ": " + loadEvent.target.error);
		                    return;
	                    }
						
						result = JSON.stringify({ name: file.name, result: evt.target.result });
	                    
	                    SendMessage(inputLoader.receiverName, inputLoader.methodName, result);
                    }
                    reader.readAsDataURL(file);
                }
        }
        
        inputLoader.receiverName = UTF8ToString(receiverName);
        inputLoader.methodName = UTF8ToString(methodName);        
        inputLoader.setAttribute("accept", UTF8ToString(extensionFilter));        
        inputLoader.click();
    }
});