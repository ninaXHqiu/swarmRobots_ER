// Unity reads the participant's signals from the page's Physio object (StreamingAssets/physio/physio.js).
mergeInto(LibraryManager.library, {
  PhysioHr: function () { return window.Physio ? window.Physio.hr() : -1; },
  PhysioEda: function () { return window.Physio ? window.Physio.eda() : -1; },
  PhysioEdaPhasic: function () { return window.Physio ? window.Physio.edaPhasic() : 0; },
  PhysioBeatCount: function () { return window.Physio ? window.Physio.beatCount() : 0; },
  PhysioSecondsSinceBeat: function () { return window.Physio ? window.Physio.secondsSinceBeat() : -1; },
  PhysioConnected: function () { return window.Physio && window.Physio.connected ? 1 : 0; },
  PhysioMarker: function (text) { if (window.Physio) { window.Physio.marker(UTF8ToString(text)); } },
  PhysioSection: function (name) { if (window.Physio) { window.Physio.sectionStart(UTF8ToString(name)); } },
  PhysioBeginRoutine: function (code, name) { if (window.Physio) { window.Physio.beginRoutine(UTF8ToString(code), UTF8ToString(name)); } },
  PhysioEndRoutine: function () { if (window.Physio) { window.Physio.endRoutine(); } }
});
