var PastePlugin = {
  ClipboardReader: function(gObj, vName) {
    var gameObjectName = UTF8ToString(gObj);
    var voidName = UTF8ToString(vName);
    if (!navigator.clipboard || !navigator.clipboard.readText) {
      console.log('Clipboard API not available on this browser.');
      SendMessage(gameObjectName, voidName, "Clipboard API not available");
      return;
    }
    navigator.clipboard.readText()
      .then(function(data) {
        SendMessage(gameObjectName, voidName, data);
      })
      .catch(function() {
        SendMessage(gameObjectName, voidName, "No text available in clipboard");
      });
  },

  ClipboardPaster: function(toCopy) {
    if (!navigator.clipboard || !navigator.clipboard.writeText) {
      console.log('Clipboard API not available on this browser.');
      return;
    }
	
	// Ensure `toCopy` is treated as a string
    var textToCopy = typeof toCopy === "number" ? UTF8ToString(toCopy) : toCopy;
	
    navigator.clipboard.writeText(textToCopy)
      .then(function() {
        console.debug("Copied to clipboard navigator: " + toCopy);
      })
      .catch(function() {
        console.debug("No text copied to clipboard");
      });
  }
};

mergeInto(LibraryManager.library, PastePlugin);
