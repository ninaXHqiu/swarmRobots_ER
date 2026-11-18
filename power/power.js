// Robot power check: connect toio robots over Bluetooth (Web Bluetooth, Chrome or Edge), read each robot's battery, and set its LED:
// 50 and above green, below 50 orange, below 20 red. Pressing a robot's colour makes that robot flash purple.
(function (global) {
  'use strict';
  var SERVICE = '10b20100-5b3b-4571-9508-cf3efcd7bbae';
  var LIGHT = '10b20103-5b3b-4571-9508-cf3efcd7bbae';      // write (with response): 0x03 duration number-of-lights light-id r g b  /  0x01 all off
  var BATTERY = '10b20108-5b3b-4571-9508-cf3efcd7bbae';    // read, notify: one byte, 0 to 100

  var GREEN = [0, 255, 0], ORANGE = [255, 90, 0], RED = [255, 0, 0], PURPLE = [160, 0, 255];

  function colorFor(power) { return power < 20 ? RED : power < 50 ? ORANGE : GREEN; }
  function lightOn(c) { return new Uint8Array([0x03, 0x00, 0x01, 0x01, c[0], c[1], c[2]]); }
  function lightOff() { return new Uint8Array([0x01]); }
  function cssColor(c) { return 'rgb(' + c[0] + ',' + c[1] + ',' + c[2] + ')'; }
  function wait(ms) { return new Promise(function (r) { setTimeout(r, ms); }); }

  // One robot. `onChange(robot)` is called whenever its power or connection changes.
  function Robot(device, onChange) {
    this.device = device; this.name = device.name || 'toio'; this.power = null; this.connected = false; this.flashing = false;
    this.onChange = onChange; this.light = null; this.battery = null; this.queue = Promise.resolve();
  }
  // Bluetooth writes to one robot go one after another
  Robot.prototype.send = function (bytes) {
    var self = this;
    this.queue = this.queue.then(function () {
      if (!self.light || !self.connected) { return; }
      // The robot's light characteristic is a plain write (with response), as the toio SDK sends it; a write without response is only a fallback.
      var ch = self.light;
      var first = ch.writeValueWithResponse ? ch.writeValueWithResponse(bytes) : ch.writeValue(bytes);
      return first.catch(function () {
        return ch.writeValueWithoutResponse ? ch.writeValueWithoutResponse(bytes) : null;
      }).catch(function (e) { if (global.console) { console.warn('light write failed', e && e.message ? e.message : e); } });
    });
    return this.queue;
  };
  Robot.prototype.currentColor = function () { return this.power === null ? null : colorFor(this.power); };
  Robot.prototype.showLevel = function () { var c = this.currentColor(); if (c && !this.flashing) { return this.send(lightOn(c)); } };
  Robot.prototype.setPower = function (p) {
    var changed = p !== this.power; this.power = p;
    if (changed) { this.showLevel(); this.onChange(this); }
  };
  // Three purple flashes, then back to the colour of its level
  Robot.prototype.flash = function () {
    var self = this;
    if (self.flashing || !self.connected) { return Promise.resolve(); }
    self.flashing = true;
    var run = Promise.resolve();
    for (var i = 0; i < 3; i++) {
      run = run.then(function () { return self.send(lightOn(PURPLE)); }).then(function () { return wait(250); })
               .then(function () { return self.send(lightOff()); }).then(function () { return wait(200); });
    }
    return run.then(function () { self.flashing = false; return self.showLevel(); });
  };
  // Connect to the robot behind `device` and start following its battery
  Robot.prototype.connect = function () {
    var self = this;
    self.device.addEventListener('gattserverdisconnected', function () { self.connected = false; self.light = null; self.onChange(self); });
    return self.device.gatt.connect().then(function (server) { return server.getPrimaryService(SERVICE); }).then(function (svc) {
      return Promise.all([svc.getCharacteristic(LIGHT), svc.getCharacteristic(BATTERY)]);
    }).then(function (ch) {
      self.light = ch[0]; self.battery = ch[1]; self.connected = true; self.onChange(self);
      return self.battery.readValue();
    }).then(function (v) {
      self.setPower(v.getUint8(0));
      self.battery.addEventListener('characteristicvaluechanged', function (e) { self.setPower(e.target.value.getUint8(0)); });
      return self.battery.startNotifications();
    }).then(function () {
      // the robot reports changes on its own; this read is a backstop
      self.poll = setInterval(function () { if (self.connected) { self.battery.readValue().then(function (v) { self.setPower(v.getUint8(0)); }).catch(function () {}); } }, 15000);
    });
  };

  // Ask for one robot (the browser shows its own device list) and connect to it
  function addRobot(bluetooth, robots, onChange) {
    return bluetooth.requestDevice({ filters: [{ services: [SERVICE] }] }).then(function (device) {
      for (var i = 0; i < robots.length; i++) { if (robots[i].device.id === device.id) { return robots[i]; } }
      var r = new Robot(device, onChange); robots.push(r); onChange(r);
      return r.connect().then(function () { return r; });
    });
  }

  // A Shimmer sensor (read only: its battery, no light to set). `client` is a connected Shimmer SDK client.
  function Shimmer(client, onChange) {
    this.client = client; this.name = (client.device && client.device.name) || 'Shimmer'; this.power = null; this.volts = null; this.charging = '';
    this.connected = true; this.isShimmer = true; this.onChange = onChange; this.poll = null;
  }
  Shimmer.prototype.currentColor = function () { return this.power === null ? null : colorFor(this.power); };
  Shimmer.prototype.flash = function () { return Promise.resolve(); };
  Shimmer.prototype.read = function () {
    var self = this;
    return self.client.getBattery().then(function (b) {
      self.power = b.percentage === null || b.percentage === undefined ? null : Math.round(b.percentage);
      self.volts = b.voltage; self.charging = b.chargingStatus || ''; self.onChange(self);
    }).catch(function (e) { if (global.console) { console.warn('shimmer battery read failed', e && e.message ? e.message : e); } });
  };
  // Ask for one Shimmer (the browser shows its own device list), connect, and follow its battery every 30 s.
  // `makeClient()` gives a new Shimmer3RClient; the sensor is only connected, it is not set up or started.
  function addShimmer(makeClient, shimmers, onChange) {
    var client = makeClient();
    return client.connect().then(function () {
      var sh = new Shimmer(client, onChange); shimmers.push(sh); onChange(sh);
      client.onDisconnect = function () { sh.connected = false; if (sh.poll) { clearInterval(sh.poll); } onChange(sh); };
      return sh.read().then(function () {
        sh.poll = setInterval(function () { if (sh.connected) { sh.read(); } }, 30000);
        return sh;
      });
    });
  }

  var api = { Shimmer: Shimmer, addShimmer: addShimmer, SERVICE: SERVICE, LIGHT: LIGHT, BATTERY: BATTERY, GREEN: GREEN, ORANGE: ORANGE, RED: RED, PURPLE: PURPLE,
              colorFor: colorFor, lightOn: lightOn, lightOff: lightOff, cssColor: cssColor, Robot: Robot, addRobot: addRobot };
  global.RobotPower = api;
  if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
})(typeof window !== 'undefined' ? window : globalThis);
