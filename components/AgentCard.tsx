'use client';

import Link from 'next/link';
import type { Agent } from '@/lib/roster';

interface AgentCardProps {
  agent: Agent;
  locale: string;
  applyLabel?: string;
}

export default function AgentCard({ agent, locale, applyLabel = 'Apply Now' }: AgentCardProps) {
  const isActive = agent.status === 'active';

  return (
    <div
      className={`agent-card relative rounded-lg p-6 ${
        isActive
          ? 'bg-elevated border-2 border-solid'
          : 'bg-elevated/50 border-2 border-dashed border-text-dim/40 cursor-pointer hover:border-text-muted/60'
      }`}
      style={{
        borderColor: isActive ? agent.color : undefined,
        boxShadow: isActive ? `0 0 20px ${agent.color}15` : undefined,
      }}
    >
      {/* Status dot */}
      <div className="absolute top-4 right-4 flex items-center gap-2">
        <span className="font-rajdhani text-xs text-text-dim uppercase tracking-wider">
          {agent.tag}
        </span>
        <span
          className={`w-2.5 h-2.5 rounded-full ${
            isActive ? 'status-dot' : 'amber-pulse'
          }`}
          style={{
            backgroundColor: isActive ? '#10b981' : '#f59e0b',
          }}
        />
      </div>

      {/* Avatar */}
      <div
        className={`w-16 h-16 rounded-full flex items-center justify-center text-2xl font-orbitron font-bold mb-4 ${
          isActive ? 'text-midnight' : 'text-text-dim'
        }`}
        style={{
          backgroundColor: isActive ? agent.color : 'transparent',
          border: isActive ? 'none' : `2px dashed ${agent.color}40`,
        }}
      >
        {isActive ? agent.name.charAt(0) : '?'}
      </div>

      {/* Name & Role */}
      <h3
        className="font-orbitron text-base font-bold tracking-wider"
        style={{ color: isActive ? agent.color : 'var(--text-dim)' }}
      >
        {agent.name}
      </h3>
      <p className="font-rajdhani text-sm text-text-muted mt-1">
        {agent.role}
        {agent.position && (
          <span className="text-text-dim ml-2">({agent.position})</span>
        )}
      </p>

      {/* Details for active agents */}
      {isActive && (
        <div className="mt-4 space-y-2">
          {agent.discord && (
            <div className="flex items-center gap-2">
              <span className="font-rajdhani text-xs text-text-dim w-16">Discord</span>
              <span className="font-exo2 text-xs text-text-muted">{agent.discord}</span>
            </div>
          )}
          {agent.rank && (
            <div className="flex items-center gap-2">
              <span className="font-rajdhani text-xs text-text-dim w-16">Rank</span>
              <span className="font-exo2 text-xs text-text-muted">{agent.rank}</span>
            </div>
          )}
          {agent.dotaId && (
            <div className="flex items-center gap-2">
              <span className="font-rajdhani text-xs text-text-dim w-16">Dota ID</span>
              <span className="font-exo2 text-xs text-text-muted">{agent.dotaId}</span>
            </div>
          )}
          {agent.email && (
            <div className="flex items-center gap-2">
              <span className="font-rajdhani text-xs text-text-dim w-16">Email</span>
              <span className="font-exo2 text-xs text-text-muted">{agent.email}</span>
            </div>
          )}
        </div>
      )}

      {/* Recruiting CTA */}
      {!isActive && (
        <Link
          href={`/${locale}/join`}
          className="inline-block mt-4 font-rajdhani text-xs font-semibold text-gold hover:text-gold/80 transition-colors"
        >
          {applyLabel} &rarr;
        </Link>
      )}
    </div>
  );
}
