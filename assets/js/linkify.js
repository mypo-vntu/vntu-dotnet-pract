document.addEventListener("DOMContentLoaded", function () {
  var URL_RE = /\bhttps?:\/\/[^\s<>"')]+/g;
  var SKIP_TAGS = { A: true, PRE: true, CODE: true, SCRIPT: true, STYLE: true, TEXTAREA: true };

  function linkifyTextNode(node) {
    var text = node.nodeValue;
    URL_RE.lastIndex = 0;
    if (!URL_RE.test(text)) return;

    var frag = document.createDocumentFragment();
    var lastIndex = 0;
    URL_RE.lastIndex = 0;
    var match;

    while ((match = URL_RE.exec(text)) !== null) {
      var url = match[0].replace(/[.,;:!?]+$/, "");
      var start = match.index;

      if (start > lastIndex) {
        frag.appendChild(document.createTextNode(text.slice(lastIndex, start)));
      }

      var a = document.createElement("a");
      a.href = url;
      a.textContent = url;
      frag.appendChild(a);

      lastIndex = start + url.length;
    }

    if (lastIndex < text.length) {
      frag.appendChild(document.createTextNode(text.slice(lastIndex)));
    }

    node.parentNode.replaceChild(frag, node);
  }

  function walk(root) {
    if (SKIP_TAGS[root.tagName]) return;

    var child = root.firstChild;
    while (child) {
      var next = child.nextSibling;
      if (child.nodeType === Node.TEXT_NODE) {
        linkifyTextNode(child);
      } else if (child.nodeType === Node.ELEMENT_NODE) {
        walk(child);
      }
      child = next;
    }
  }

  var main = document.querySelector("main");
  if (main) walk(main);
});
